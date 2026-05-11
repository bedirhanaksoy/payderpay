using System.Net.Http.Json;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Dtos.External;

namespace PayderPay.Infrastructure.ExternalServices;

public class DebtProviderClient : IDebtProviderClient
{
    private readonly HttpClient _httpClient;

    public DebtProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DebtProviderQueryResponse> QueryDebtAsync(DebtProviderQueryRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/mock/debt/query", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DebtProviderQueryResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Mock debt service returned an empty response.");
    }
}
