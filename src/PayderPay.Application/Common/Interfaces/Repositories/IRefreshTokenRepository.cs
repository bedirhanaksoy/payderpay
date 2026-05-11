using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    void Update(RefreshToken refreshToken);
    Task RevokeAllForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
