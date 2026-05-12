using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface IDebtRepository
{
    Task AddAsync(Debt debt, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyList<Debt> debts, CancellationToken cancellationToken = default);
    Task<Debt?> GetCurrentBySubscriptionAndDebtIdAsync(Guid subscriptionId, Guid debtId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Debt>> GetCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task SoftDeleteCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    void Update(Debt debt);
}
