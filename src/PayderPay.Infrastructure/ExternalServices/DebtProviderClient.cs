using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Settings;
using PayderPay.Application.Dtos.External;

namespace PayderPay.Infrastructure.ExternalServices;

public class DebtProviderClient : IDebtProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly ExternalServiceSettings _settings;

    public DebtProviderClient(HttpClient httpClient, IOptions<ExternalServiceSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<DebtProviderQueryResponse> QueryDebtAsync(DebtProviderQueryRequest request, CancellationToken cancellationToken = default)
    {
        var path = NormalizeRelativePath(_settings.DebtQueryPath, "api/mock/debt/query");
        var requestUri = BuildRequestUri(_httpClient.BaseAddress, path);
        using var response = await _httpClient.PostAsJsonAsync(requestUri, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Debt provider request failed with status code {(int)response.StatusCode} for '{requestUri}'. Response: {Truncate(responseBody, 500)}",
                null,
                response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<DebtProviderQueryResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Mock debt service returned an empty response.");
    }

    private static string NormalizeRelativePath(string? configuredPath, string fallbackPath)
    {
        var value = string.IsNullOrWhiteSpace(configuredPath) ? fallbackPath : configuredPath;
        return value.Trim().TrimStart('/');
    }

    private static Uri BuildRequestUri(Uri? baseAddress, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        if (baseAddress is null)
        {
            throw new InvalidOperationException("Debt provider base URL is not configured.");
        }

        return new Uri(baseAddress, path);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "<empty>";
        }

        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
