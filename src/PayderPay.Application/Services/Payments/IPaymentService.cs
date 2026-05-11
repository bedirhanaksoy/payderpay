using PayderPay.Application.Common.Pagination;
using PayderPay.Application.Dtos.Payments;

namespace PayderPay.Application.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(Guid subscriptionId, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentHistoryItemResponse>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentHistoryItemResponse>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<PagedResult<PaymentHistoryItemResponse>> GetBySubscriptionPagedAsync(Guid subscriptionId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<PaymentHistoryItemResponse>> GetByCustomerPagedAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default);
}
