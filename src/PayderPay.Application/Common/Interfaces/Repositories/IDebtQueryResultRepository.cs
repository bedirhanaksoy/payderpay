using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface IDebtQueryResultRepository
{
    Task AddAsync(DebtQueryResult debtQueryResult, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyList<DebtQueryResult> debtQueryResults, CancellationToken cancellationToken = default);
    Task<DebtQueryResult?> GetCurrentBySubscriptionAndDebtIdAsync(Guid subscriptionId, Guid debtId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DebtQueryResult>> GetCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task SoftDeleteCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    void Update(DebtQueryResult debtQueryResult);
}
