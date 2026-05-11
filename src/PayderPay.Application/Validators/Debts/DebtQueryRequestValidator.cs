using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Debts;

namespace PayderPay.Application.Validators.Debts;

public class DebtQueryRequestValidator : RequestValidator<DebtQueryRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(DebtQueryRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.PeriodYear.HasValue ^ request.PeriodMonth.HasValue)
        {
            errors[nameof(request.PeriodYear)] = ["PeriodYear and PeriodMonth must be provided together."];
        }

        if (request.PeriodYear is < 2000 or > 3000)
        {
            errors[nameof(request.PeriodYear)] = ["PeriodYear must be between 2000 and 3000."];
        }

        if (request.PeriodMonth is < 1 or > 12)
        {
            errors[nameof(request.PeriodMonth)] = ["PeriodMonth must be between 1 and 12."];
        }

        return errors;
    }
}
