using System.ComponentModel.DataAnnotations;

namespace PayderPay.Application.DTOs.Debts;

public class DebtQueryRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
}
