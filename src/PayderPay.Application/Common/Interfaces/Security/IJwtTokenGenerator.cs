using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Security;

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(Customer customer);
    string GenerateRefreshTokenValue();
    string HashRefreshToken(string rawToken);
    DateTime GetRefreshTokenExpiryUtc();
}
