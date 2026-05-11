using PayderPay.Application.Common.Pagination;
using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<PagedResult<Payment>> GetBySubscriptionPagedAsync(Guid subscriptionId, PageRequest page, CancellationToken cancellationToken = default);
    Task<PagedResult<Payment>> GetByCustomerPagedAsync(Guid customerId, PageRequest page, CancellationToken cancellationToken = default);
    Task<bool> HasSuccessfulPaymentForPeriodAsync(Guid subscriptionId, int periodYear, int periodMonth, CancellationToken cancellationToken = default);
    Task<bool> HasSuccessfulPaymentForDebtIdAsync(Guid debtId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
