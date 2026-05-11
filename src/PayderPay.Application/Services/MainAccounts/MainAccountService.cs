using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Dtos.MainAccounts;
using PayderPay.Application.Common.Exceptions;

namespace PayderPay.Application.Services;

public class MainAccountService : IMainAccountService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMainAccountRepository _mainAccountRepository;

    public MainAccountService(
        ICustomerRepository customerRepository,
        IMainAccountRepository mainAccountRepository)
    {
        _customerRepository = customerRepository;
        _mainAccountRepository = mainAccountRepository;
    }

    public async Task<MainAccountResponse> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException($"Customer '{customerId}' was not found.");

        _ = customer;

        var mainAccount = await _mainAccountRepository.GetByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException($"Main account for customer '{customerId}' was not found.");

        return new MainAccountResponse
        {
            CustomerId = mainAccount.CustomerId,
            Iban = mainAccount.Iban,
            Balance = mainAccount.Balance,
            CreatedAtUtc = mainAccount.CreatedAtUtc,
            UpdatedAtUtc = mainAccount.UpdatedAtUtc
        };
    }
}
