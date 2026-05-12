using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class DebtRepository : IDebtRepository
{
    private readonly PayderPayDbContext _context;

    public DebtRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Debt debt, CancellationToken cancellationToken = default)
    {
        await _context.Debts.AddAsync(debt, cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyList<Debt> debts, CancellationToken cancellationToken = default)
    {
        if (debts.Count == 0)
        {
            return;
        }

        await _context.Debts.AddRangeAsync(debts, cancellationToken);
    }

    public async Task<Debt?> GetCurrentBySubscriptionAndDebtIdAsync(
        Guid subscriptionId,
        Guid debtId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Debts
            .Where(x => x.SubscriptionId == subscriptionId && x.DebtId == debtId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Debt>> GetCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Debts
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task SoftDeleteCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var currentItems = await _context.Debts
            .Where(x => x.SubscriptionId == subscriptionId)
            .ToListAsync(cancellationToken);

        if (currentItems.Count == 0)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        foreach (var item in currentItems)
        {
            item.IsDeleted = true;
            item.DeletedAtUtc = utcNow;
            item.UpdatedAtUtc = utcNow;
        }
    }

    public void Update(Debt debt)
    {
        _context.Debts.Update(debt);
    }
}
