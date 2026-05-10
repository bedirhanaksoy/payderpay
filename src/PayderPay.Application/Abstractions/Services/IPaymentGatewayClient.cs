using PayderPay.Application.DTOs.External;

namespace PayderPay.Application.Abstractions.Services;

public interface IPaymentGatewayClient
{
    Task<PaymentGatewayResponse> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default);
}
