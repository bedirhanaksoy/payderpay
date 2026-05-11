using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class DebtQueryResultRepository : IDebtQueryResultRepository
{
    private readonly PayderPayDbContext _context;

    public DebtQueryResultRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DebtQueryResult debtQueryResult, CancellationToken cancellationToken = default)
    {
        await _context.DebtQueryResults.AddAsync(debtQueryResult, cancellationToken);
    }

    public async Task<DebtQueryResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DebtQueryResults
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<DebtQueryResult?> GetLatestBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.DebtQueryResults
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderByDescending(x => x.QueriedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DebtQueryResult>> GetHistoryBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.DebtQueryResults
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderByDescending(x => x.QueriedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
