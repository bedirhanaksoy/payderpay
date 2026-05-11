using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PayderPay.Api.IntegrationTests;

/// <summary>
/// Integration-test only authentication handler. Always succeeds and stamps a
/// synthetic customer principal so the [Authorize] attributes on controllers
/// don't block test flows that pre-date the JWT auth integration.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    private static readonly Guid TestCustomerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, TestCustomerId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, TestCustomerId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "integration-test@payderpay.local"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
