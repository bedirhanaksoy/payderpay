using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PayderPayDbContext _context;

    public PaymentRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderByDescending(x => x.PaymentDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(x => x.Subscription)
            .Where(x => x.Subscription.CustomerId == customerId)
            .OrderByDescending(x => x.PaymentDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasSuccessfulPaymentForPeriodAsync(Guid subscriptionId, int periodYear, int periodMonth, CancellationToken cancellationToken = default)
    {
        return await _context.Payments.AnyAsync(
            x => x.SubscriptionId == subscriptionId &&
                 x.PeriodYear == periodYear &&
                 x.PeriodMonth == periodMonth &&
                 x.Status == PaymentStatus.Successful,
            cancellationToken);
    }

    public async Task<bool> HasSuccessfulPaymentForDebtIdAsync(Guid debtId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments.AnyAsync(
            x => x.DebtId == debtId &&
                 x.Status == PaymentStatus.Successful,
            cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
    }
}
