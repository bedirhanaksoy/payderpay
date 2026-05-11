using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Customers;

namespace PayderPay.Application.Validators.Customers;

public class CreateCustomerRequestValidator : RequestValidator<CreateCustomerRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(CreateCustomerRequest request)
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

        if (request.InitialMainAccountBalance < 0)
        {
            errors[nameof(request.InitialMainAccountBalance)] = ["Initial main account balance cannot be negative."];
        }

        return errors;
    }
}
