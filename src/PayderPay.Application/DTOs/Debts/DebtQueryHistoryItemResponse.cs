namespace PayderPay.Application.Dtos.Debts;

public class DebtQueryHistoryItemResponse
{
    public Guid DebtId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string SubscriberNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateTime QueriedAtUtc { get; set; }
    public string? ProviderRef { get; set; }
    public string ProviderName { get; set; } = string.Empty;
}
