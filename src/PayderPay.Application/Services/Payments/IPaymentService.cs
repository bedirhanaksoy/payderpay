using PayderPay.Application.Dtos.Payments;

namespace PayderPay.Application.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(Guid subscriptionId, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentHistoryItemResponse>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentHistoryItemResponse>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
