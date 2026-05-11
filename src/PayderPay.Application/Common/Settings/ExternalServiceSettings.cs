namespace PayderPay.Application.Common.Settings;

public sealed class ExternalServiceSettings
{
    public const string SectionName = "ExternalServices";

    public string MockApiBaseUrl { get; set; } = "http://localhost:5270";
}
