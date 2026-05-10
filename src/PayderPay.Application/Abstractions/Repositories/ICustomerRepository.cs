using PayderPay.Domain.Entities;

namespace PayderPay.Application.Abstractions.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    void Update(Customer customer);
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
