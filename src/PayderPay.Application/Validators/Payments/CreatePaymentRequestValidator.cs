using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Payments;

namespace PayderPay.Application.Validators.Payments;

public class CreatePaymentRequestValidator : RequestValidator<CreatePaymentRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(CreatePaymentRequest request)
    {
        if (request.DebtId == Guid.Empty)
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.DebtId)] = ["DebtId is required."]
            };
        }

        return NoErrors;
    }
}
