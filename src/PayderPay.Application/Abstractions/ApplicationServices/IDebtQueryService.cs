using PayderPay.Application.DTOs.Debts;

namespace PayderPay.Application.Abstractions.ApplicationServices;

public interface IDebtQueryService
{
    Task<DebtQueryResponse> QueryAsync(Guid subscriptionId, DebtQueryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DebtQueryHistoryItemResponse>> GetHistoryAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
