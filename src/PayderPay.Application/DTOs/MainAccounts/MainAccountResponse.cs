namespace PayderPay.Application.Dtos.MainAccounts;

public class MainAccountResponse
{
    public Guid CustomerId { get; set; }
    public string Iban { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
