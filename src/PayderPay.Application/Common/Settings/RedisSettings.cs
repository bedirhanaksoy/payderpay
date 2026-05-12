namespace PayderPay.Application.Common.Settings;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "localhost:6379";
    public string KeyPrefix { get; set; } = "payderpay";
    public int DebtTtlSeconds { get; set; } = 60;
    public int SummaryTtlSeconds { get; set; } = 60;
    public int SubscriptionsTtlSeconds { get; set; } = 60;
}
