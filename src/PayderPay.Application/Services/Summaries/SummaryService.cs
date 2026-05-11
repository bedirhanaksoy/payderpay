using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common;
using PayderPay.Application.Dtos.Summaries;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Services;

public class SummaryService : ISummaryService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPaymentRepository _paymentRepository;

    public SummaryService(
        ICustomerRepository customerRepository,
        ISubscriptionRepository subscriptionRepository,
        IPaymentRepository paymentRepository)
    {
        _customerRepository = customerRepository;
        _subscriptionRepository = subscriptionRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<DashboardSummaryResponse> GetDashboardAsync(Guid customerId, int year, int month, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        var unpaidSubscriptions = await GetUnpaidSubscriptionsAsync(customerId, year, month, cancellationToken);
        var payments = await _paymentRepository.GetByCustomerAsync(customerId, cancellationToken);

        var successfulTotal = payments
            .Where(x => x.PeriodYear == year && x.PeriodMonth == month && x.Status == PaymentStatus.Successful)
            .Sum(x => x.Amount);

        var activeSubscriptions = await _subscriptionRepository.GetByCustomerAsync(customerId, cancellationToken);
        var activeCount = activeSubscriptions.Count(x => x.Status == SubscriptionStatus.Active);

        return new DashboardSummaryResponse
        {
            ActiveSubscriptionCount = activeCount,
            UnpaidThisMonthCount = unpaidSubscriptions.Count,
            SuccessfulPaymentsThisMonthTotal = successfulTotal,
            UnpaidSubscriptions = unpaidSubscriptions
        };
    }

    public async Task<IReadOnlyList<UnpaidSubscriptionResponse>> GetUnpaidSubscriptionsAsync(Guid customerId, int year, int month, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException($"Customer '{customerId}' was not found.");

        _ = customer;

        var subscriptions = await _subscriptionRepository.GetByCustomerAsync(customerId, cancellationToken);
        var activeSubscriptions = subscriptions.Where(x => x.Status == SubscriptionStatus.Active).ToList();

        var unpaid = new List<UnpaidSubscriptionResponse>();

        foreach (var subscription in activeSubscriptions)
        {
            var hasSuccess = await _paymentRepository.HasSuccessfulPaymentForPeriodAsync(
                subscription.Id,
                year,
                month,
                cancellationToken);

            if (hasSuccess)
            {
                continue;
            }

            unpaid.Add(new UnpaidSubscriptionResponse
            {
                SubscriptionId = subscription.Id,
                CustomerId = subscription.CustomerId,
                SubscriptionType = subscription.SubscriptionType,
                ProviderName = subscription.ProviderName,
                SubscriberNumber = subscription.SubscriberNumber,
                PeriodYear = year,
                PeriodMonth = month,
                DueDate = BillingDateHelper.CalculateDueDate(year, month, subscription.DueDayOfMonth)
            });
        }

        return unpaid;
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 2000 or > 3000)
        {
            throw new BadRequestException("Year must be between 2000 and 3000.");
        }

        if (month is < 1 or > 12)
        {
            throw new BadRequestException("Month must be between 1 and 12.");
        }
    }
}
