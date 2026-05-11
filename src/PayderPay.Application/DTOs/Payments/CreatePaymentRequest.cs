using System.ComponentModel.DataAnnotations;

namespace PayderPay.Application.Dtos.Payments;

public class CreatePaymentRequest
{
    [Required]
    public Guid DebtQueryResultId { get; set; }
}
