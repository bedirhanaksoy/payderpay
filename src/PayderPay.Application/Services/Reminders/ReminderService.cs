using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayderPay.Application.Common.Helpers;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Notifications;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Settings;
using PayderPay.Application.Dtos.External;
using PayderPay.Application.Dtos.Reminders;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Services.Reminders;

public class ReminderService : IReminderService
{
    private const string DueReminderType = "due_reminder_3d";
    private const string DefaultCurrency = "TRY";

    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDebtProviderClient _debtProviderClient;
    private readonly IRedisCacheStore _redisCacheStore;
    private readonly TimeSpan _debtCacheTtl;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly INotificationQueueRepository _notificationQueueRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        ISubscriptionRepository subscriptionRepository,
        IDebtProviderClient debtProviderClient,
        IRedisCacheStore redisCacheStore,
        IOptions<RedisSettings> redisSettings,
        IInvoiceRepository invoiceRepository,
        INotificationQueueRepository notificationQueueRepository,
        IPaymentRepository paymentRepository,
        IEmailNotificationService emailNotificationService,
        IUnitOfWork unitOfWork,
        ILogger<ReminderService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _debtProviderClient = debtProviderClient;
        _redisCacheStore = redisCacheStore;
        var ttl = redisSettings.Value.DebtTtlSeconds;
        _debtCacheTtl = TimeSpan.FromSeconds(ttl > 0 ? ttl : 60);
        _invoiceRepository = invoiceRepository;
        _notificationQueueRepository = notificationQueueRepository;
        _paymentRepository = paymentRepository;
        _emailNotificationService = emailNotificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SendReminderResultResponse>> SendDuePaymentRemindersAsync(
        DateOnly referenceDate,
        int leadDays = 3,
        CancellationToken cancellationToken = default)
    {
        var runResult = await RunInvoiceSyncAndDeliveryAsync(referenceDate, leadDays, 3, cancellationToken);

        return
        [
            new SendReminderResultResponse
            {
                CustomerId = Guid.Empty,
                SubscriptionId = Guid.Empty,
                PeriodYear = referenceDate.Year,
                PeriodMonth = referenceDate.Month,
                Sent = runResult.NotificationDelivery.SentCount > 0,
                Message = $"Sync subscriptions: {runResult.InvoiceSync.ProcessedSubscriptions}, Sent mails: {runResult.NotificationDelivery.SentCount}",
                ProcessedAtUtc = DateTime.UtcNow
            }
        ];
    }

    public async Task<ReminderRunResponse> RunInvoiceSyncAndDeliveryAsync(
        DateOnly referenceDate,
        int leadDays = 3,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        var syncResult = await RunInvoiceSyncAsync(referenceDate, leadDays, cancellationToken);
        var deliveryResult = await RunNotificationDeliveryAsync(referenceDate, maxAttempts, cancellationToken);

        return new ReminderRunResponse
        {
            InvoiceSync = syncResult,
            NotificationDelivery = deliveryResult
        };
    }

    public async Task<InvoiceSyncRunResultResponse> RunInvoiceSyncAsync(
        DateOnly referenceDate,
        int leadDays = 3,
        CancellationToken cancellationToken = default)
    {
        var result = new InvoiceSyncRunResultResponse();
        var safeLeadDays = Math.Max(0, leadDays);
        var activeSubscriptions = await _subscriptionRepository.GetActiveAsync(cancellationToken);

        result.ProcessedSubscriptions = activeSubscriptions.Count;

        foreach (var subscription in activeSubscriptions)
        {
            try
            {
                await SyncSubscriptionInvoicesAsync(subscription, referenceDate, result, cancellationToken);
                result.SucceededSubscriptions++;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                result.ErrorCount++;
                result.ShortCircuited = true;

                _logger.LogError(
                    ex,
                    "Invoice sync short-circuited due to timeout for subscription {SubscriptionId}.",
                    subscription.Id);
                break;
            }
            catch (HttpRequestException ex) when (IsProviderUnavailable(ex))
            {
                result.ErrorCount++;
                result.ShortCircuited = true;

                _logger.LogError(
                    ex,
                    "Invoice sync short-circuited due to provider unavailability for subscription {SubscriptionId}.",
                    subscription.Id);
                break;
            }
            catch (Exception ex)
            {
                result.FailedSubscriptions++;
                result.ErrorCount++;

                _logger.LogWarning(
                    ex,
                    "Invoice sync failed for subscription {SubscriptionId}. Continuing with next subscription.",
                    subscription.Id);
            }
        }

        await ScheduleReminderQueueAsync(referenceDate, safeLeadDays, result, cancellationToken);

        _logger.LogInformation(
            "Invoice sync completed. Processed={Processed}, Success={Success}, Failed={Failed}, Created={Created}, Updated={Updated}, MarkedPaid={MarkedPaid}, Queued={Queued}, Errors={Errors}, ShortCircuited={ShortCircuited}",
            result.ProcessedSubscriptions,
            result.SucceededSubscriptions,
            result.FailedSubscriptions,
            result.CreatedInvoices,
            result.UpdatedInvoices,
            result.MarkedPaidInvoices,
            result.QueuedNotifications,
            result.ErrorCount,
            result.ShortCircuited);

        return result;
    }

    public async Task<NotificationDeliveryRunResultResponse> RunNotificationDeliveryAsync(
        DateOnly referenceDate,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        var result = new NotificationDeliveryRunResultResponse();
        var safeMaxAttempts = Math.Max(1, maxAttempts);

        var pendingItems = await _notificationQueueRepository.GetPendingForDeliveryAsync(referenceDate, safeMaxAttempts, cancellationToken);

        foreach (var pending in pendingItems)
        {
            result.ProcessedItems++;

            try
            {
                var processed = await ProcessNotificationItemAsync(pending.Id, referenceDate, safeMaxAttempts, cancellationToken);

                switch (processed)
                {
                    case DeliveryOutcome.Sent:
                        result.SentCount++;
                        break;
                    case DeliveryOutcome.PendingForRetry:
                        result.FailedCount++;
                        result.PendingForRetryCount++;
                        break;
                    case DeliveryOutcome.Failed:
                        result.FailedCount++;
                        break;
                    case DeliveryOutcome.MaxRetryReached:
                        result.FailedCount++;
                        result.MaxRetryReachedCount++;
                        break;
                    case DeliveryOutcome.SkippedClosedOrPaid:
                        result.SkippedClosedOrPaidCount++;
                        break;
                    case DeliveryOutcome.LockNotAcquired:
                        break;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                _logger.LogError(ex, "Notification delivery failed for queue item {QueueItemId}.", pending.Id);
            }
        }

        result.CleanedQueueItemCount = await _notificationQueueRepository.DeleteCompletedByTypeBeforeDateAsync(
            DueReminderType,
            referenceDate,
            cancellationToken);

        _logger.LogInformation(
            "Notification delivery completed. Processed={Processed}, Sent={Sent}, Failed={Failed}, RetryPending={RetryPending}, MaxRetry={MaxRetry}, Skipped={Skipped}, Cleaned={Cleaned}, Errors={Errors}",
            result.ProcessedItems,
            result.SentCount,
            result.FailedCount,
            result.PendingForRetryCount,
            result.MaxRetryReachedCount,
            result.SkippedClosedOrPaidCount,
            result.CleanedQueueItemCount,
            result.ErrorCount);

        return result;
    }

    private async Task SyncSubscriptionInvoicesAsync(
        Subscription subscription,
        DateOnly referenceDate,
        InvoiceSyncRunResultResponse result,
        CancellationToken cancellationToken)
    {
        DebtProviderQueryResponse providerResponse;
        providerResponse = await GetProviderResponseWithCacheAsync(subscription.SubscriberNumber, cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var currentExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var debt in providerResponse.Debts)
        {
            var externalId = debt.DebtId.ToString("D");
            currentExternalIds.Add(externalId);

            var existing = await _invoiceRepository.GetBySubscriptionAndExternalIdAsync(subscription.Id, externalId, cancellationToken);
            if (existing is null)
            {
                var invoice = new Invoice
                {
                    SubscriptionId = subscription.Id,
                    UserId = subscription.CustomerId,
                    ExternalId = externalId,
                    DueDate = debt.DueDate,
                    Amount = debt.Amount,
                    Currency = DefaultCurrency,
                    Status = ResolveInvoiceStatus(debt.DueDate, referenceDate),
                    FetchedAt = nowUtc,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                await _invoiceRepository.AddAsync(invoice, cancellationToken);
                result.CreatedInvoices++;
                continue;
            }

            existing.DueDate = debt.DueDate;
            existing.Amount = debt.Amount;
            existing.Currency = DefaultCurrency;
            existing.Status = ResolveInvoiceStatus(debt.DueDate, referenceDate);
            existing.FetchedAt = nowUtc;
            existing.UpdatedAt = nowUtc;

            _invoiceRepository.Update(existing);
            result.UpdatedInvoices++;
        }

        var knownInvoices = await _invoiceRepository.GetBySubscriptionAsync(subscription.Id, cancellationToken);
        foreach (var knownInvoice in knownInvoices)
        {
            if (knownInvoice.Status != InvoiceStatus.Unpaid)
            {
                continue;
            }

            if (currentExternalIds.Contains(knownInvoice.ExternalId))
            {
                continue;
            }

            knownInvoice.Status = InvoiceStatus.Paid;
            knownInvoice.UpdatedAt = nowUtc;
            _invoiceRepository.Update(knownInvoice);
            result.MarkedPaidInvoices++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ScheduleReminderQueueAsync(
        DateOnly referenceDate,
        int leadDays,
        InvoiceSyncRunResultResponse result,
        CancellationToken cancellationToken)
    {
        var safeLeadDays = Math.Max(1, leadDays);
        var fromDate = referenceDate.AddDays(1);
        var toDate = referenceDate.AddDays(safeLeadDays);
        var dueInvoices = await _invoiceRepository.GetUnpaidDueBetweenAsync(fromDate, toDate, cancellationToken);

        foreach (var invoice in dueInvoices)
        {
            try
            {
                var scheduledFor = referenceDate;

                var queueItem = new NotificationQueueItem
                {
                    InvoiceId = invoice.Id,
                    UserId = invoice.UserId,
                    NotificationType = DueReminderType,
                    IdempotencyKey = BuildIdempotencyKey(invoice.Id, referenceDate),
                    Status = NotificationQueueStatus.Pending,
                    ScheduledFor = scheduledFor,
                    Attempts = 0,
                    CreatedAt = DateTime.UtcNow
                };

                var inserted = await _notificationQueueRepository.TryEnqueueAsync(queueItem, cancellationToken);
                if (inserted)
                {
                    result.QueuedNotifications++;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                _logger.LogWarning(ex, "Queue scheduling failed for invoice {InvoiceId}.", invoice.Id);
            }
        }
    }

    private async Task<DeliveryOutcome> ProcessNotificationItemAsync(
        Guid queueItemId,
        DateOnly today,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var lockAcquired = await _notificationQueueRepository.TryMarkSendingAsync(queueItemId, cancellationToken);
            if (!lockAcquired)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return DeliveryOutcome.LockNotAcquired;
            }

            var queueItem = await _notificationQueueRepository.GetByIdWithRelationsAsync(queueItemId, cancellationToken);
            if (queueItem is null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return DeliveryOutcome.LockNotAcquired;
            }

            if (await ShouldSkipBecauseAlreadyClosedOrPaidAsync(queueItem.Invoice, cancellationToken))
            {
                queueItem.Status = NotificationQueueStatus.Failed;
                queueItem.LastError = "already_paid_or_closed";
                _notificationQueueRepository.Update(queueItem);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return DeliveryOutcome.SkippedClosedOrPaid;
            }

            var daysLeft = Math.Max(0, queueItem.Invoice.DueDate.DayNumber - today.DayNumber);
            var dispatch = await _emailNotificationService.SendInvoiceDueReminderAsync(
                new InvoiceDueReminderRequest(
                    ToEmail: queueItem.User.Email,
                    CustomerName: queueItem.User.FullName,
                    ProviderName: queueItem.Invoice.Subscription.ProviderName,
                    SubscriberNumber: queueItem.Invoice.Subscription.SubscriberNumber,
                    Amount: queueItem.Invoice.Amount,
                    Currency: queueItem.Invoice.Currency,
                    DueDate: queueItem.Invoice.DueDate,
                    DaysLeft: daysLeft),
                cancellationToken);

            if (dispatch.Sent)
            {
                queueItem.Status = NotificationQueueStatus.Sent;
                queueItem.SentAt = dispatch.SentAtUtc ?? DateTime.UtcNow;
                queueItem.LastError = null;
                _notificationQueueRepository.Update(queueItem);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return DeliveryOutcome.Sent;
            }

            queueItem.Attempts += 1;
            queueItem.LastError = dispatch.FailureReason ?? "mail_send_failed";

            if (queueItem.Attempts >= maxAttempts)
            {
                queueItem.Status = NotificationQueueStatus.Failed;
                _notificationQueueRepository.Update(queueItem);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return DeliveryOutcome.MaxRetryReached;
            }

            queueItem.Status = NotificationQueueStatus.Pending;
            _notificationQueueRepository.Update(queueItem);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return DeliveryOutcome.PendingForRetry;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<bool> ShouldSkipBecauseAlreadyClosedOrPaidAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        if (invoice.Status != InvoiceStatus.Unpaid)
        {
            return true;
        }

        if (!Guid.TryParse(invoice.ExternalId, out var debtId))
        {
            return false;
        }

        return await _paymentRepository.HasSuccessfulPaymentForDebtIdAsync(debtId, cancellationToken);
    }

    private static InvoiceStatus ResolveInvoiceStatus(DateOnly dueDate, DateOnly referenceDate)
    {
        if (dueDate < referenceDate)
        {
            return InvoiceStatus.Overdue;
        }

        return InvoiceStatus.Unpaid;
    }

    private static bool IsProviderUnavailable(HttpRequestException exception)
    {
        if (!exception.StatusCode.HasValue)
        {
            return true;
        }

        return (int)exception.StatusCode.Value >= 500;
    }

    private static string BuildIdempotencyKey(Guid invoiceId, DateOnly runDate)
        => $"{invoiceId:D}__{DueReminderType}__{runDate:yyyyMMdd}";

    private async Task<DebtProviderQueryResponse> GetProviderResponseWithCacheAsync(
        string subscriberNumber,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyFactory.DebtBySubscriber(subscriberNumber);
        var cached = await _redisCacheStore.GetAsync<DebtProviderQueryResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return NormalizeResponse(cached, subscriberNumber);
        }

        DebtProviderQueryResponse liveResponse;
        try
        {
            liveResponse = await _debtProviderClient.QueryDebtAsync(new DebtProviderQueryRequest
            {
                SubscriberNumber = subscriberNumber
            }, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            liveResponse = new DebtProviderQueryResponse
            {
                SubscriberNumber = subscriberNumber,
                Debts = Array.Empty<DebtProviderDebtItem>()
            };
        }

        var normalized = NormalizeResponse(liveResponse, subscriberNumber);
        await _redisCacheStore.SetAsync(cacheKey, normalized, _debtCacheTtl, cancellationToken);
        return normalized;
    }

    private static DebtProviderQueryResponse NormalizeResponse(DebtProviderQueryResponse response, string fallbackSubscriberNumber)
    {
        return new DebtProviderQueryResponse
        {
            SubscriberNumber = string.IsNullOrWhiteSpace(response.SubscriberNumber)
                ? fallbackSubscriberNumber
                : response.SubscriberNumber,
            Debts = response.Debts ?? Array.Empty<DebtProviderDebtItem>()
        };
    }

    private enum DeliveryOutcome
    {
        LockNotAcquired = 0,
        Sent = 1,
        PendingForRetry = 2,
        Failed = 3,
        MaxRetryReached = 4,
        SkippedClosedOrPaid = 5
    }
}
