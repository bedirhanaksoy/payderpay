using System.Net.Http.Json;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Dtos.External;

namespace PayderPay.Infrastructure.ExternalServices;

public class PaymentGatewayClient : IPaymentGatewayClient
{
    private readonly HttpClient _httpClient;

    public PaymentGatewayClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentGatewayResponse> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/payment/process", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaymentGatewayResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Mock payment service returned an empty response.");
    }
}
