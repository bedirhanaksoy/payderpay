using PayderPay.Application.Abstractions.ApplicationServices;
using PayderPay.Application.Abstractions.Repositories;
using PayderPay.Application.Abstractions.Services;
using PayderPay.Application.DTOs.External;
using PayderPay.Application.DTOs.Payments;
using PayderPay.Application.Exceptions;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDebtQueryResultRepository _debtQueryResultRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGatewayClient _paymentGatewayClient;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        ISubscriptionRepository subscriptionRepository,
        IDebtQueryResultRepository debtQueryResultRepository,
        IPaymentRepository paymentRepository,
        IPaymentGatewayClient paymentGatewayClient,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _debtQueryResultRepository = debtQueryResultRepository;
        _paymentRepository = paymentRepository;
        _paymentGatewayClient = paymentGatewayClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentResponse> CreateAsync(Guid subscriptionId, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{subscriptionId}' was not found.");

        if (subscription.Status != SubscriptionStatus.Active)
        {
            throw new BadRequestException("Payment can only be created for active subscriptions.");
        }

        var debt = await _debtQueryResultRepository.GetByIdAsync(request.DebtQueryResultId, cancellationToken)
            ?? throw new NotFoundException($"Debt query result '{request.DebtQueryResultId}' was not found.");

        if (debt.SubscriptionId != subscription.Id)
        {
            throw new BadRequestException("Debt query result does not belong to the requested subscription.");
        }

        var hasSuccessfulPayment = await _paymentRepository.HasSuccessfulPaymentForPeriodAsync(
            subscriptionId,
            debt.PeriodYear,
            debt.PeriodMonth,
            cancellationToken);

        if (hasSuccessfulPayment)
        {
            throw new ConflictException("A successful payment already exists for this subscription and period.");
        }

        var gatewayResponse = await _paymentGatewayClient.ProcessPaymentAsync(
            new PaymentGatewayRequest
            {
                SubscriptionId = subscription.Id,
                Amount = debt.Amount,
                PeriodYear = debt.PeriodYear,
                PeriodMonth = debt.PeriodMonth
            },
            cancellationToken);

        var payment = new Payment
        {
            SubscriptionId = subscription.Id,
            Amount = debt.Amount,
            PaymentDateUtc = DateTime.UtcNow,
            PeriodYear = debt.PeriodYear,
            PeriodMonth = debt.PeriodMonth,
            Status = gatewayResponse.IsSuccessful ? PaymentStatus.Successful : PaymentStatus.Failed,
            ExternalTransactionId = gatewayResponse.ExternalTransactionId,
            FailureReason = gatewayResponse.FailureReason
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PaymentResponse
        {
            Id = payment.Id,
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
            SubscriptionId = payment.SubscriptionId,
            Amount = payment.Amount,
            PaymentDateUtc = payment.PaymentDateUtc,
            PeriodYear = payment.PeriodYear,
            PeriodMonth = payment.PeriodMonth,
            Status = payment.Status
        };
    }
}
