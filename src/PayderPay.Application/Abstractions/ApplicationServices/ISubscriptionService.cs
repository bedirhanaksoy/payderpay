using PayderPay.Application.DTOs.Subscriptions;

namespace PayderPay.Application.Abstractions.ApplicationServices;

public interface ISubscriptionService
{
    Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<SubscriptionResponse> UpdateAsync(Guid id, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionResponse>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionResponse>> GetActiveAsync(CancellationToken cancellationToken = default);
}
