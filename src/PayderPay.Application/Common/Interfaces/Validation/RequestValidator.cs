namespace PayderPay.Application.Common.Interfaces.Validation;

public abstract class RequestValidator<TRequest> : IRequestValidator
    where TRequest : class
{
    public Type RequestType => typeof(TRequest);

    public IReadOnlyDictionary<string, string[]> Validate(object request)
    {
        if (request is not TRequest typedRequest)
        {
            return new Dictionary<string, string[]>
            {
                [string.Empty] = ["Invalid request payload type."]
            };
        }

        return Validate(typedRequest);
    }

    protected abstract IReadOnlyDictionary<string, string[]> Validate(TRequest request);

    protected static IReadOnlyDictionary<string, string[]> NoErrors =>
        new Dictionary<string, string[]>();
}
