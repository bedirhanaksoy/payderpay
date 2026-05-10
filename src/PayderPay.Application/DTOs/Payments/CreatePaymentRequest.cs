using System.ComponentModel.DataAnnotations;

namespace PayderPay.Application.DTOs.Payments;

public class CreatePaymentRequest
{
    [Required]
    public Guid DebtQueryResultId { get; set; }
}
