using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Auth;

namespace PayderPay.Application.Validators.Auth;

public class RegisterRequestValidator : RequestValidator<RegisterRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors[nameof(request.FullName)] = ["Full name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors[nameof(request.Email)] = ["Email is required."];
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            errors[nameof(request.PhoneNumber)] = ["Phone number is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors[nameof(request.Password)] = ["Password is required."];
        }
        else if (request.Password.Length < 8)
        {
            errors[nameof(request.Password)] = ["Password must be at least 8 characters."];
        }

        if (request.InitialMainAccountBalance < 0)
        {
            errors[nameof(request.InitialMainAccountBalance)] = ["Initial main account balance cannot be negative."];
        }

        return errors;
    }
}
