using PayderPay.Application.Dtos.Summaries;

namespace PayderPay.Application.Services;

public interface ISummaryService
{
    Task<DashboardSummaryResponse> GetDashboardAsync(Guid customerId, int year, int month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnpaidSubscriptionResponse>> GetUnpaidSubscriptionsAsync(Guid customerId, int year, int month, CancellationToken cancellationToken = default);
}
