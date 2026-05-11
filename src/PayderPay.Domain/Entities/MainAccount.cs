using PayderPay.Domain.Common;

namespace PayderPay.Domain.Entities;

public class MainAccount : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string Iban { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    public Customer Customer { get; set; } = null!;
}
