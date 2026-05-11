namespace PayderPay.Application.Common.Settings;

public sealed class ReminderJobSettings
{
    public const string SectionName = "ReminderJob";

    public bool Enabled { get; set; } = true;
    public int LeadDays { get; set; } = 3;
    public int MaxAttempts { get; set; } = 3;
    public string InvoiceSyncCron { get; set; } = "0 2 * * *";
    public string NotificationDeliveryCron { get; set; } = "0 9 * * *";
}
