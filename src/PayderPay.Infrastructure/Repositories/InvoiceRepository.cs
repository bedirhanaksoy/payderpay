using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly PayderPayDbContext _context;

    public InvoiceRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetBySubscriptionAndExternalIdAsync(
        Guid subscriptionId,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .FirstOrDefaultAsync(
                x => x.SubscriptionId == subscriptionId && x.ExternalId == externalId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetBySubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetUnpaidDueBetweenAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Where(x => x.Status == InvoiceStatus.Unpaid && x.DueDate >= fromDate && x.DueDate <= toDate)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(x => x.User)
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
    }

    public void Update(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
    }
}
