using PayderPay.Application.Dtos.Debts;

namespace PayderPay.Application.Services;

public interface IDebtQueryService
{
    Task<DebtQueryResponse> QueryAsync(Guid subscriptionId, DebtQueryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DebtQueryHistoryItemResponse>> GetHistoryAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
