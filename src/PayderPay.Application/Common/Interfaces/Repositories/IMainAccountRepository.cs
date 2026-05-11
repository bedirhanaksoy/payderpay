using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface IMainAccountRepository
{
    Task<MainAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIbanAsync(string iban, CancellationToken cancellationToken = default);
    Task AddAsync(MainAccount mainAccount, CancellationToken cancellationToken = default);
    void Update(MainAccount mainAccount);
}
