using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface IDebtQueryResultRepository
{
    Task AddAsync(DebtQueryResult debtQueryResult, CancellationToken cancellationToken = default);
    Task<DebtQueryResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DebtQueryResult?> GetLatestBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DebtQueryResult>> GetHistoryBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
