using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Dtos.Subscriptions;

namespace PayderPay.Application.Validators.Subscriptions;

public class UpdateSubscriptionRequestValidator : RequestValidator<UpdateSubscriptionRequest>
{
    protected override IReadOnlyDictionary<string, string[]> Validate(UpdateSubscriptionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

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
