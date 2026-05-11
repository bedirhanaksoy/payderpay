namespace PayderPay.Application.Common.Settings;

public sealed class ReminderJobSettings
{
    public const string SectionName = "ReminderJob";

    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public int LeadDays { get; set; } = 3;
}
