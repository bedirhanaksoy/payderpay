using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class MainAccountRepository : IMainAccountRepository
{
    private readonly PayderPayDbContext _context;

    public MainAccountRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task<MainAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.MainAccounts
            .FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
    }

    public async Task<bool> ExistsByIbanAsync(string iban, CancellationToken cancellationToken = default)
    {
        return await _context.MainAccounts.AnyAsync(x => x.Iban == iban, cancellationToken);
    }

    public async Task AddAsync(MainAccount mainAccount, CancellationToken cancellationToken = default)
    {
        await _context.MainAccounts.AddAsync(mainAccount, cancellationToken);
    }

    public void Update(MainAccount mainAccount)
    {
        _context.MainAccounts.Update(mainAccount);
    }
}
