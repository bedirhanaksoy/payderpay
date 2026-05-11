namespace PayderPay.Application.Dtos.External;

public class DebtProviderQueryResponse
{
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public string? ProviderRef { get; set; }
}
