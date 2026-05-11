namespace PayderPay.Application.Dtos.Summaries;

public class DashboardSummaryResponse
{
    public int ActiveSubscriptionCount { get; set; }
    public int UnpaidThisMonthCount { get; set; }
    public decimal SuccessfulPaymentsThisMonthTotal { get; set; }
    public IReadOnlyList<UnpaidSubscriptionResponse> UnpaidSubscriptions { get; set; } = Array.Empty<UnpaidSubscriptionResponse>();
}
