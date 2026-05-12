using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly PayderPayDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
        {
            return;
        }

        if (_currentTransaction is not null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryAcquireAdvisoryLockAsync(long key, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
        {
            // Non-relational provider (e.g. in-memory tests) — locking is a no-op,
            // assume acquired so production logic can proceed without modification.
            return true;
        }

        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_try_advisory_lock(@key)";
        var p = cmd.CreateParameter();
        p.ParameterName = "@key";
        p.Value = key;
        cmd.Parameters.Add(p);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is bool acquired && acquired;
    }

    public async Task ReleaseAdvisoryLockAsync(long key, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
        {
            return;
        }

        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            // Connection already returned to the pool — advisory lock is released
            // automatically when the underlying session ends or DISCARD ALL is issued.
            return;
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_advisory_unlock(@key)";
        var p = cmd.CreateParameter();
        p.ParameterName = "@key";
        p.Value = key;
        cmd.Parameters.Add(p);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}
