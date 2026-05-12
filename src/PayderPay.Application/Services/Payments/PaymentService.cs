using Microsoft.Extensions.Logging;
using PayderPay.Application.Common.Helpers;
using PayderPay.Application.Common.Interfaces.Notifications;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Pagination;
using PayderPay.Application.Dtos.External;
using PayderPay.Application.Dtos.Payments;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDebtQueryService _debtQueryService;
    private readonly IDebtRepository _debtRepository;
    private readonly IMainAccountRepository _mainAccountRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IPaymentGatewayClient _paymentGatewayClient;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IRedisCacheStore _redisCacheStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ISubscriptionRepository subscriptionRepository,
        IDebtQueryService debtQueryService,
        IDebtRepository debtRepository,
        IMainAccountRepository mainAccountRepository,
        IPaymentRepository paymentRepository,
        INotificationLogRepository notificationLogRepository,
        IPaymentGatewayClient paymentGatewayClient,
        IEmailNotificationService emailNotificationService,
        IRedisCacheStore redisCacheStore,
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _debtQueryService = debtQueryService;
        _debtRepository = debtRepository;
        _mainAccountRepository = mainAccountRepository;
        _paymentRepository = paymentRepository;
        _notificationLogRepository = notificationLogRepository;
        _paymentGatewayClient = paymentGatewayClient;
        _emailNotificationService = emailNotificationService;
        _redisCacheStore = redisCacheStore;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentResponse> CreateAsync(Guid subscriptionId, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{subscriptionId}' was not found.");

        if (subscription.Status != SubscriptionStatus.Active)
        {
            throw new BadRequestException("Payment can only be created for active subscriptions.");
        }

        var debtSnapshotBeforeValidation = await _debtRepository.GetCurrentBySubscriptionAndDebtIdAsync(
            subscriptionId,
            request.DebtId,
            cancellationToken)
            ?? await ThrowDebtConflictAsync(request.DebtId, cancellationToken);

        var refreshedDebtState = await _debtQueryService.QueryLiveAsync(subscriptionId, cancellationToken);
        var validatedDebt = refreshedDebtState.Debts.FirstOrDefault(x => x.DebtId == request.DebtId);

        if (validatedDebt is null)
        {
            throw new ConflictException("Debt changed. Please refresh and try again.");
        }

        if (validatedDebt.Amount != debtSnapshotBeforeValidation.Amount)
        {
            throw new ConflictException("Debt amount changed. Please refresh and try again.");
        }

        var hasSuccessfulPayment = await _paymentRepository.HasSuccessfulPaymentForDebtIdAsync(
            request.DebtId,
            cancellationToken);

        if (hasSuccessfulPayment)
        {
            throw new ConflictException("A successful payment already exists for this debt.");
        }

        var currentDebtSnapshot = await _debtRepository.GetCurrentBySubscriptionAndDebtIdAsync(
            subscriptionId,
            request.DebtId,
            cancellationToken)
            ?? throw new ConflictException("Debt changed. Please refresh and try again.");

        var mainAccount = await _mainAccountRepository.GetByCustomerIdAsync(subscription.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Main account for customer '{subscription.CustomerId}' was not found.");

        if (mainAccount.Balance < validatedDebt.Amount)
        {
            throw new ConflictException("Insufficient balance in the main account.");
        }

        var gatewayResponse = await _paymentGatewayClient.ProcessPaymentAsync(
            new PaymentGatewayRequest
            {
                DebtId = request.DebtId,
                Amount = validatedDebt.Amount
            },
            cancellationToken);

        var payment = new Payment
        {
            DebtId = request.DebtId,
            SubscriptionId = subscription.Id,
            Amount = validatedDebt.Amount,
            PaymentDateUtc = DateTime.UtcNow,
            PeriodYear = validatedDebt.PeriodYear,
            PeriodMonth = validatedDebt.PeriodMonth,
            Status = gatewayResponse.IsSuccessful ? PaymentStatus.Successful : PaymentStatus.Failed,
            ExternalTransactionId = gatewayResponse.ExternalTransactionId,
            FailureReason = gatewayResponse.FailureReason
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (gatewayResponse.IsSuccessful)
            {
                mainAccount.Balance -= validatedDebt.Amount;
                _mainAccountRepository.Update(mainAccount);

                currentDebtSnapshot.IsDeleted = true;
                currentDebtSnapshot.DeletedAtUtc = DateTime.UtcNow;
                currentDebtSnapshot.UpdatedAtUtc = DateTime.UtcNow;
                _debtRepository.Update(currentDebtSnapshot);
            }

            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        if (gatewayResponse.IsSuccessful)
        {
            await InvalidateCachesAfterSuccessfulPaymentAsync(subscription, payment, cancellationToken);
        }

        await SendReceiptEmailAsync(subscription, payment, cancellationToken);

        return new PaymentResponse
        {
            Id = payment.Id,
            DebtId = payment.DebtId,
            SubscriptionId = payment.SubscriptionId,
            Amount = payment.Amount,
            PaymentDateUtc = payment.PaymentDateUtc,
            PeriodYear = payment.PeriodYear,
            PeriodMonth = payment.PeriodMonth,
            Status = payment.Status,
            ExternalTransactionId = payment.ExternalTransactionId,
            FailureReason = payment.FailureReason
        };
    }

    private async Task InvalidateCachesAfterSuccessfulPaymentAsync(
        Subscription subscription,
        Payment payment,
        CancellationToken cancellationToken)
    {
        await _redisCacheStore.RemoveAsync(CacheKeyFactory.DebtBySubscriber(subscription.SubscriberNumber), cancellationToken);
        await _redisCacheStore.RemoveAsync(CacheKeyFactory.DashboardSummary(subscription.CustomerId, payment.PeriodYear, payment.PeriodMonth), cancellationToken);
        await _redisCacheStore.RemoveAsync(CacheKeyFactory.UnpaidSubscriptions(subscription.CustomerId, payment.PeriodYear, payment.PeriodMonth), cancellationToken);
        await _redisCacheStore.RemoveAsync(CacheKeyFactory.SubscriptionsAll(subscription.CustomerId), cancellationToken);
        await _redisCacheStore.RemoveAsync(CacheKeyFactory.SubscriptionsActiveAll(), cancellationToken);
    }

    private async Task<Debt> ThrowDebtConflictAsync(Guid debtId, CancellationToken cancellationToken)
    {
        if (await _paymentRepository.HasSuccessfulPaymentForDebtIdAsync(debtId, cancellationToken))
        {
            throw new ConflictException("A successful payment already exists for this debt.");
        }

        throw new ConflictException("Debt changed. Please refresh and try again.");
    }

    private async Task SendReceiptEmailAsync(Subscription subscription, Payment payment, CancellationToken cancellationToken)
    {
        try
        {
            var dispatch = await _emailNotificationService.SendPaymentReceiptAsync(
                new PaymentReceiptRequest(
                    ToEmail: subscription.Customer.Email,
                    CustomerName: subscription.Customer.FullName,
                    SubscriptionType: subscription.SubscriptionType,
                    ProviderName: subscription.ProviderName,
                    SubscriberNumber: subscription.SubscriberNumber,
                    Amount: payment.Amount,
                    PaidAtUtc: payment.PaymentDateUtc,
                    PeriodYear: payment.PeriodYear,
                    PeriodMonth: payment.PeriodMonth,
                    Status: payment.Status,
                    ExternalTransactionId: payment.ExternalTransactionId,
                    FailureReason: payment.FailureReason),
                cancellationToken);

            var log = new NotificationLog
            {
                CustomerId = subscription.CustomerId,
                SubscriptionId = subscription.Id,
                PeriodYear = payment.PeriodYear,
                PeriodMonth = payment.PeriodMonth,
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
        catch (Exception ex)
        {
            // Receipt mailing must never break the payment response.
            _logger.LogError(ex, "Failed to send/persist payment receipt for payment {PaymentId}", payment.Id);
        }
    }

    public async Task<IReadOnlyList<PaymentHistoryItemResponse>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{subscriptionId}' was not found.");

        _ = subscription;

        var payments = await _paymentRepository.GetBySubscriptionAsync(subscriptionId, cancellationToken);

        return payments
            .Select(x => ToHistoryItem(x, subscription.SubscriberNumber))
            .ToList();
    }

    public async Task<IReadOnlyList<PaymentHistoryItemResponse>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetByCustomerAsync(customerId, cancellationToken);
        return payments
            .Select(x => ToHistoryItem(x, x.Subscription.SubscriberNumber))
            .ToList();
    }

    public async Task<PagedResult<PaymentHistoryItemResponse>> GetBySubscriptionPagedAsync(
        Guid subscriptionId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{subscriptionId}' was not found.");

        var paged = await _paymentRepository.GetBySubscriptionPagedAsync(subscriptionId, page, cancellationToken);
        var responses = paged.Items
            .Select(x => ToHistoryItem(x, subscription.SubscriberNumber))
            .ToList();
        return paged.Map(responses);
    }

    public async Task<PagedResult<PaymentHistoryItemResponse>> GetByCustomerPagedAsync(
        Guid customerId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var paged = await _paymentRepository.GetByCustomerPagedAsync(customerId, page, cancellationToken);
        var responses = paged.Items
            .Select(x => ToHistoryItem(x, x.Subscription.SubscriberNumber))
            .ToList();
        return paged.Map(responses);
    }

    private static PaymentHistoryItemResponse ToHistoryItem(Payment payment, string subscriberNumber)
    {
        return new PaymentHistoryItemResponse
        {
            Id = payment.Id,
            DebtId = payment.DebtId,
            SubscriptionId = payment.SubscriptionId,
            SubscriberNumber = subscriberNumber,
            Amount = payment.Amount,
            PaymentDateUtc = payment.PaymentDateUtc,
            PeriodYear = payment.PeriodYear,
            PeriodMonth = payment.PeriodMonth,
            Status = payment.Status
        };
    }
}
