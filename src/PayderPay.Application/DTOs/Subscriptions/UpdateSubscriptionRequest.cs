using System.ComponentModel.DataAnnotations;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Dtos.Subscriptions;

public class UpdateSubscriptionRequest
{
    [Required]
    public SubscriptionType SubscriptionType { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SubscriberNumber { get; set; } = string.Empty;

    [Required]
    public SubscriptionStatus Status { get; set; }

    [Range(1, 31)]
    public int DueDayOfMonth { get; set; }
}
