using Microsoft.Extensions.Logging;
using PayderPay.Application.Common.Interfaces.Notifications;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.External;
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
    private readonly IDebtQueryResultRepository _debtQueryResultRepository;
    private readonly IMainAccountRepository _mainAccountRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IPaymentGatewayClient _paymentGatewayClient;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ISubscriptionRepository subscriptionRepository,
        IDebtQueryService debtQueryService,
        IDebtQueryResultRepository debtQueryResultRepository,
        IMainAccountRepository mainAccountRepository,
        IPaymentRepository paymentRepository,
        INotificationLogRepository notificationLogRepository,
        IPaymentGatewayClient paymentGatewayClient,
        IEmailNotificationService emailNotificationService,
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _debtQueryService = debtQueryService;
        _debtQueryResultRepository = debtQueryResultRepository;
        _mainAccountRepository = mainAccountRepository;
        _paymentRepository = paymentRepository;
        _notificationLogRepository = notificationLogRepository;
        _paymentGatewayClient = paymentGatewayClient;
        _emailNotificationService = emailNotificationService;
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

        var debtSnapshotBeforeValidation = await _debtQueryResultRepository.GetCurrentBySubscriptionAndDebtIdAsync(
            subscriptionId,
            request.DebtId,
            cancellationToken)
            ?? await ThrowDebtConflictAsync(request.DebtId, cancellationToken);

        var refreshedDebtState = await _debtQueryService.QueryAsync(subscriptionId, cancellationToken);
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

        var currentDebtSnapshot = await _debtQueryResultRepository.GetCurrentBySubscriptionAndDebtIdAsync(
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
                _debtQueryResultRepository.Update(currentDebtSnapshot);
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

    private async Task<DebtQueryResult> ThrowDebtConflictAsync(Guid debtId, CancellationToken cancellationToken)
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
            .Select(ToHistoryItem)
            .ToList();
    }

    public async Task<IReadOnlyList<PaymentHistoryItemResponse>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetByCustomerAsync(customerId, cancellationToken);
        return payments.Select(ToHistoryItem).ToList();
    }

    private static PaymentHistoryItemResponse ToHistoryItem(Payment payment)
    {
        return new PaymentHistoryItemResponse
        {
            Id = payment.Id,
            DebtId = payment.DebtId,
            SubscriptionId = payment.SubscriptionId,
            Amount = payment.Amount,
            PaymentDateUtc = payment.PaymentDateUtc,
            PeriodYear = payment.PeriodYear,
            PeriodMonth = payment.PeriodMonth,
            Status = payment.Status
        };
    }
}
