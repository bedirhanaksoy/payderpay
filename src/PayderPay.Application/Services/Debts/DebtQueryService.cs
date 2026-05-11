using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common;
using PayderPay.Application.Dtos.Debts;
using PayderPay.Application.Dtos.External;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Services;

public class DebtQueryService : IDebtQueryService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDebtQueryResultRepository _debtQueryResultRepository;
    private readonly IDebtProviderClient _debtProviderClient;
    private readonly IUnitOfWork _unitOfWork;

    public DebtQueryService(
        ISubscriptionRepository subscriptionRepository,
        IDebtQueryResultRepository debtQueryResultRepository,
        IDebtProviderClient debtProviderClient,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _debtQueryResultRepository = debtQueryResultRepository;
        _debtProviderClient = debtProviderClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<DebtQueryResponse> QueryAsync(Guid subscriptionId, DebtQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PeriodYear.HasValue ^ request.PeriodMonth.HasValue)
        {
            throw new BadRequestException("PeriodYear and PeriodMonth must be provided together.");
        }

        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{subscriptionId}' was not found.");

        if (subscription.Status != SubscriptionStatus.Active)
        {
            throw new BadRequestException("Debt query can only be performed for active subscriptions.");
        }

        var (periodYear, periodMonth) = BillingDateHelper.ResolvePeriod(request.PeriodYear, request.PeriodMonth);

        var providerRequest = new DebtProviderQueryRequest
        {
            SubscriptionId = subscription.Id,
            SubscriberNumber = subscription.SubscriberNumber,
            ProviderName = subscription.ProviderName,
            PeriodYear = periodYear,
            PeriodMonth = periodMonth,
            DueDayOfMonth = subscription.DueDayOfMonth
        };

        var providerResponse = await _debtProviderClient.QueryDebtAsync(providerRequest, cancellationToken);

        var result = new DebtQueryResult
        {
            SubscriptionId = subscription.Id,
            Amount = providerResponse.Amount,
            DueDate = providerResponse.DueDate,
            PeriodYear = providerResponse.PeriodYear,
            PeriodMonth = providerResponse.PeriodMonth,
            QueriedAtUtc = DateTime.UtcNow,
            ProviderRef = providerResponse.ProviderRef
        };

        await _debtQueryResultRepository.AddAsync(result, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DebtQueryResponse
        {
            SubscriptionId = result.SubscriptionId,
            Amount = result.Amount,
            DueDate = result.DueDate,
            PeriodYear = result.PeriodYear,
            PeriodMonth = result.PeriodMonth,
            QueriedAtUtc = result.QueriedAtUtc,
            ProviderRef = result.ProviderRef
        };
    }

    public async Task<IReadOnlyList<DebtQueryHistoryItemResponse>> GetHistoryAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{subscriptionId}' was not found.");

        _ = subscription;

        var items = await _debtQueryResultRepository.GetHistoryBySubscriptionAsync(subscriptionId, cancellationToken);

        return items
            .Select(x => new DebtQueryHistoryItemResponse
            {
                Id = x.Id,
                SubscriptionId = x.SubscriptionId,
                Amount = x.Amount,
                DueDate = x.DueDate,
                PeriodYear = x.PeriodYear,
                PeriodMonth = x.PeriodMonth,
                QueriedAtUtc = x.QueriedAtUtc,
                ProviderRef = x.ProviderRef
            })
            .ToList();
    }
}
