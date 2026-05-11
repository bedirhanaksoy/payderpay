using System.Security.Cryptography;
using System.Text;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.Security;

namespace PayderPay.Infrastructure.Security;

public class IbanGenerator : IIbanGenerator
{
    private const int MaxAttempts = 50;
    private readonly IMainAccountRepository _mainAccountRepository;

    public IbanGenerator(IMainAccountRepository mainAccountRepository)
    {
        _mainAccountRepository = mainAccountRepository;
    }

    public async Task<string> GenerateUniqueIbanAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var iban = GenerateCandidate();
            var exists = await _mainAccountRepository.ExistsByIbanAsync(iban, cancellationToken);
            if (!exists)
            {
                return iban;
            }
        }

        throw new InvalidOperationException("Failed to generate a unique IBAN after multiple attempts.");
    }

    private static string GenerateCandidate()
    {
        var builder = new StringBuilder("TR", 26);

        for (var i = 0; i < 24; i++)
        {
            builder.Append(RandomNumberGenerator.GetInt32(0, 10));
        }

        return builder.ToString();
    }
}
