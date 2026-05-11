using PayderPay.Application.Dtos.Customers;

namespace PayderPay.Application.Dtos.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public CustomerResponse Customer { get; set; } = new();
}
