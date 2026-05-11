namespace PayderPay.Application.Dtos.External;

public class DebtProviderQueryResponse
{
    public string SubscriberNumber { get; set; } = string.Empty;
    public IReadOnlyList<DebtProviderDebtItem> Debts { get; set; } = Array.Empty<DebtProviderDebtItem>();
}

public class DebtProviderDebtItem
{
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public string? ProviderRef { get; set; }
    public string ProviderName { get; set; } = string.Empty;
}
