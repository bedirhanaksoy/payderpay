using PayderPay.Application.DTOs.External;

namespace PayderPay.Application.Abstractions.Services;

public interface IDebtProviderClient
{
    Task<DebtProviderQueryResponse> QueryDebtAsync(DebtProviderQueryRequest request, CancellationToken cancellationToken = default);
}
