using PayderPay.Application.Common.Pagination;
using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<Subscription>> GetByCustomerPagedAsync(Guid customerId, PageRequest page, CancellationToken cancellationToken = default);
    Task<PagedResult<Subscription>> GetActivePagedAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetDueSubscriptionsAsync(DateOnly referenceDate, int leadDays, CancellationToken cancellationToken = default);
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    void Update(Subscription subscription);
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
