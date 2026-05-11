namespace PayderPay.Application.Common.Interfaces.Security;

public interface IIbanGenerator
{
    Task<string> GenerateUniqueIbanAsync(CancellationToken cancellationToken = default);
}
