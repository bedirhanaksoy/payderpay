namespace PayderPay.Application.Common.Settings;

public sealed class ExternalServiceSettings
{
    public const string SectionName = "ExternalServices";

    public string MockApiBaseUrl { get; set; } = "http://localhost:8090";
    public string DebtQueryPath { get; set; } = "api/mock/debt/query";
    public string PaymentProcessPath { get; set; } = "api/payment/process";
}
