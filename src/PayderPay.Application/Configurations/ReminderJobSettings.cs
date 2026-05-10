namespace PayderPay.Application.Configurations;

public sealed class ReminderJobSettings
{
    public const string SectionName = "ReminderJob";

    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public int LeadDays { get; set; } = 3;
}
