using Microsoft.Extensions.Logging;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Notifications;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Dtos.External;
using PayderPay.Application.Dtos.Reminders;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Services.Reminders;

public class ReminderService : IReminderService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IDebtProviderClient _debtProviderClient;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        ISubscriptionRepository subscriptionRepository,
        IPaymentRepository paymentRepository,
        INotificationLogRepository notificationLogRepository,
        IDebtProviderClient debtProviderClient,
        IEmailNotificationService emailNotificationService,
        IUnitOfWork unitOfWork,
        ILogger<ReminderService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _paymentRepository = paymentRepository;
        _notificationLogRepository = notificationLogRepository;
        _debtProviderClient = debtProviderClient;
        _emailNotificationService = emailNotificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SendReminderResultResponse>> SendDuePaymentRemindersAsync(
        DateOnly referenceDate,
        int leadDays = 3,
        CancellationToken cancellationToken = default)
    {
        var safeLeadDays = leadDays < 0 ? 0 : leadDays;
        var subscriptions = await _subscriptionRepository.GetDueSubscriptionsAsync(referenceDate, safeLeadDays, cancellationToken);
        var results = new List<SendReminderResultResponse>(subscriptions.Count);

        var periodYear = referenceDate.Year;
        var periodMonth = referenceDate.Month;

        foreach (var subscription in subscriptions)
        {
            var processedAt = DateTime.UtcNow;
            var result = new SendReminderResultResponse
            {
                CustomerId = subscription.CustomerId,
                SubscriptionId = subscription.Id,
                PeriodYear = periodYear,
                PeriodMonth = periodMonth,
                ProcessedAtUtc = processedAt
            };

            // Skip if already paid for this period.
            var subscriptionPayments = await _paymentRepository.GetBySubscriptionAsync(subscription.Id, cancellationToken);
            var alreadyPaid = subscriptionPayments.Any(x =>
                x.Status == PaymentStatus.Successful &&
                x.PeriodYear == periodYear &&
                x.PeriodMonth == periodMonth);
            if (alreadyPaid)
            {
                result.Sent = false;
                result.Message = "Bu dönem için ödeme zaten yapılmış. Hatırlatma atlanmıştır.";
                results.Add(result);
                continue;
            }

            // Skip if a reminder for this period was already sent (idempotency).
            var alreadyReminded = await _notificationLogRepository.HasSentReminderForPeriodAsync(
                subscription.Id, periodYear, periodMonth, cancellationToken);
            if (alreadyReminded)
            {
                result.Sent = false;
                result.Message = "Bu dönem için hatırlatma daha önce gönderilmiş.";
                results.Add(result);
                continue;
            }

            // Pull current debt amount from the 3rd-party debt provider.
            decimal amount;
            DateOnly dueDate;
            try
            {
                var debtResponse = await _debtProviderClient.QueryDebtAsync(new DebtProviderQueryRequest
                {
                    SubscriberNumber = subscription.SubscriberNumber
                }, cancellationToken);

                var debt = debtResponse.Debts
                    .OrderBy(x => x.DueDate)
                    .ThenBy(x => x.PeriodYear)
                    .ThenBy(x => x.PeriodMonth)
                    .FirstOrDefault();

                if (debt is null)
                {
                    result.Sent = false;
                    result.Message = "Abonelik için ödenmemiş borç bulunamadı.";
                    results.Add(result);
                    continue;
                }

                amount = debt.Amount;
                dueDate = debt.DueDate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Debt query failed for subscription {SubscriptionId}; reminder skipped.", subscription.Id);
                result.Sent = false;
                result.Message = $"Borç sorgulama başarısız oldu: {ex.Message}";
                results.Add(result);
                continue;
            }

            var dispatch = await _emailNotificationService.SendUpcomingPaymentReminderAsync(
                new UpcomingPaymentReminderRequest(
                    ToEmail: subscription.Customer.Email,
                    CustomerName: subscription.Customer.FullName,
                    SubscriptionType: subscription.SubscriptionType,
                    ProviderName: subscription.ProviderName,
                    SubscriberNumber: subscription.SubscriberNumber,
                    Amount: amount,
                    DueDate: dueDate,
                    PeriodYear: periodYear,
                    PeriodMonth: periodMonth),
                cancellationToken);

            await PersistNotificationLogAsync(subscription, periodYear, periodMonth, dispatch, cancellationToken);

            result.Sent = dispatch.Sent;
            result.Message = dispatch.Sent
                ? "Hatırlatma e-postası gönderildi."
                : $"E-posta gönderilemedi: {dispatch.FailureReason}";
            results.Add(result);
        }

        return results;
    }

    private async Task PersistNotificationLogAsync(
        Subscription subscription,
        int periodYear,
        int periodMonth,
        EmailDispatchResult dispatch,
        CancellationToken cancellationToken)
    {
        var log = new NotificationLog
        {
            CustomerId = subscription.CustomerId,
            SubscriptionId = subscription.Id,
            PeriodYear = periodYear,
            PeriodMonth = periodMonth,
            Channel = NotificationChannel.Email,
            Recipient = subscription.Customer.Email,
            Subject = dispatch.Subject,
            Message = dispatch.Body,
            Status = dispatch.Sent ? NotificationStatus.Sent : NotificationStatus.Failed,
            SentAtUtc = dispatch.SentAtUtc,
            FailureReason = dispatch.FailureReason
        };

        await _notificationLogRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
