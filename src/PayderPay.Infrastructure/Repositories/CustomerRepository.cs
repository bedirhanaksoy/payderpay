using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Pagination;
using PayderPay.Domain.Entities;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly PayderPayDbContext _context;

    public CustomerRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Customers
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Customers.AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Customer>> GetAllPagedAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);
        if (total == 0)
        {
            return PagedResult<Customer>.Empty(page);
        }

        var items = await query
            .OrderBy(x => x.FullName)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Customer>.From(items, total, page);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public void Update(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
    }
}
