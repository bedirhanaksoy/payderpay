using System.ComponentModel.DataAnnotations;

namespace PayderPay.MockApi.Contracts;

public class MockDebtQueryRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SubscriberNumber { get; set; } = string.Empty;

    [Range(2000, 3000)]
    public int PeriodYear { get; set; }

    [Range(1, 12)]
    public int PeriodMonth { get; set; }

    [Range(1, 31)]
    public int DueDayOfMonth { get; set; } = 15;
}
