using System.ComponentModel.DataAnnotations;

namespace PayderPay.Application.Dtos.Debts;

public class DebtQueryRequest
{
    [Range(2000, 3000)]
    public int? PeriodYear { get; set; }

    [Range(1, 12)]
    public int? PeriodMonth { get; set; }
}
