using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly PayderPayDbContext _context;

    public RefreshTokenRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        return _context.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }

    public async Task RevokeAllForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var activeTokens = await _context.RefreshTokens
            .Where(x => x.CustomerId == customerId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = utcNow;
        }
    }
}
