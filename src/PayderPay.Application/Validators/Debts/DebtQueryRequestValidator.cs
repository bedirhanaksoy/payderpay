using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Debts;

namespace PayderPay.Application.Validators.Debts;

public class DebtQueryRequestValidator : RequestValidator<DebtQueryRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(DebtQueryRequest request)
    {
        _ = request;
        return NoErrors;
    }
}
