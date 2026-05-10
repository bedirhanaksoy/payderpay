using PayderPay.Application.DTOs.Summaries;

namespace PayderPay.Application.Abstractions.ApplicationServices;

public interface ISummaryService
{
    Task<DashboardSummaryResponse> GetDashboardAsync(Guid customerId, int year, int month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnpaidSubscriptionResponse>> GetUnpaidSubscriptionsAsync(Guid customerId, int year, int month, CancellationToken cancellationToken = default);
}
