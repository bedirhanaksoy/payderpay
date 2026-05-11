using PayderPay.Application.DTOs.MainAccounts;

namespace PayderPay.Application.Abstractions.ApplicationServices;

public interface IMainAccountService
{
    Task<MainAccountResponse> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
