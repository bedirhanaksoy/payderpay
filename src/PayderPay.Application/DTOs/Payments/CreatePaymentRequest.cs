using System.ComponentModel.DataAnnotations;

namespace PayderPay.Application.DTOs.Payments;

public class CreatePaymentRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [Range(2000, 3000)]
    public int PeriodYear { get; set; }

    [Range(1, 12)]
    public int PeriodMonth { get; set; }
}
