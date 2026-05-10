namespace PayderPay.Application.DTOs.Debts;

public class DebtQueryResponse
{
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateTime QueriedAtUtc { get; set; }
    public string? ProviderRef { get; set; }
}
