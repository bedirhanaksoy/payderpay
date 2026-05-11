using PayderPay.Application.Dtos.External;

namespace PayderPay.Application.Common.Interfaces.External;

public interface IDebtProviderClient
{
    Task<DebtProviderQueryResponse> QueryDebtAsync(DebtProviderQueryRequest request, CancellationToken cancellationToken = default);
}
