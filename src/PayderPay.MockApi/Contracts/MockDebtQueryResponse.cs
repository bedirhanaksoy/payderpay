namespace PayderPay.MockApi.Contracts;

public class MockDebtQueryResponse
{
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public string ProviderRef { get; set; } = string.Empty;
}
