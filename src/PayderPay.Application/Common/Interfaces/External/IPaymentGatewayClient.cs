using PayderPay.Application.Dtos.External;

namespace PayderPay.Application.Common.Interfaces.External;

public interface IPaymentGatewayClient
{
    Task<PaymentGatewayResponse> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default);
}
