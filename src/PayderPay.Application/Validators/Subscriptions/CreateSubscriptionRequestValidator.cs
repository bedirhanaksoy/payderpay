using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Subscriptions;

namespace PayderPay.Application.Validators.Subscriptions;

public class CreateSubscriptionRequestValidator : RequestValidator<CreateSubscriptionRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(CreateSubscriptionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CustomerId == Guid.Empty)
        {
            errors[nameof(request.CustomerId)] = ["CustomerId is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ProviderName))
        {
            errors[nameof(request.ProviderName)] = ["Provider name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.SubscriberNumber))
        {
            errors[nameof(request.SubscriberNumber)] = ["Subscriber number is required."];
        }

        return errors;
    }
}
