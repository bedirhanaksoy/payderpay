using PayderPay.Application.Dtos.Debts;

namespace PayderPay.Application.Services;

public interface IDebtQueryService
{
    Task<DebtQueryResponse> QueryAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<DebtQueryResponse> QueryLiveAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<DebtQueryResponse> GetCurrentAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
