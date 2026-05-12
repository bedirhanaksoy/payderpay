namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Session-scoped advisory lock used to serialize concurrent operations on the same key
    // (e.g. preventing duplicate payment processing for a given DebtId).
    Task<bool> TryAcquireAdvisoryLockAsync(long key, CancellationToken cancellationToken = default);
    Task ReleaseAdvisoryLockAsync(long key, CancellationToken cancellationToken = default);
}
