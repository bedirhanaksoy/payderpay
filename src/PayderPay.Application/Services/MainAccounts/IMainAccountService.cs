using PayderPay.Application.Dtos.MainAccounts;

namespace PayderPay.Application.Services;

public interface IMainAccountService
{
    Task<MainAccountResponse> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
