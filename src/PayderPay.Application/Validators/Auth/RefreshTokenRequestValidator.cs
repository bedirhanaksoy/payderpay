using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Auth;

namespace PayderPay.Application.Validators.Auth;

public class RefreshTokenRequestValidator : RequestValidator<RefreshTokenRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.RefreshToken)] = ["Refresh token is required."]
            };
        }

        return NoErrors;
    }
}
