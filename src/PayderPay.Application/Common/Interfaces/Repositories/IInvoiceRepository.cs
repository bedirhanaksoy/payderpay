using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetBySubscriptionAndExternalIdAsync(Guid subscriptionId, string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetUnpaidDueBetweenAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    void Update(Invoice invoice);
}
