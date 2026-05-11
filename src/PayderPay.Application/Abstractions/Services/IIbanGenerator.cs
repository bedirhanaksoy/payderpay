namespace PayderPay.Application.Abstractions.Services;

public interface IIbanGenerator
{
    Task<string> GenerateUniqueIbanAsync(CancellationToken cancellationToken = default);
}
