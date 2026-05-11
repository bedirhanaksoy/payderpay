using PayderPay.Domain.Common;

namespace PayderPay.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public Customer Customer { get; set; } = null!;

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
