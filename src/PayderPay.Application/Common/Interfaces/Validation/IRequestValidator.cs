namespace PayderPay.Application.Common.Interfaces.Validation;

public interface IRequestValidator
{
    Type RequestType { get; }
    IReadOnlyDictionary<string, string[]> Validate(object request);
}
