using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Auth;

namespace PayderPay.Application.Validators.Auth;

public class LoginRequestValidator : RequestValidator<LoginRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors[nameof(request.Email)] = ["Email is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors[nameof(request.Password)] = ["Password is required."];
        }

        return errors;
    }
}
