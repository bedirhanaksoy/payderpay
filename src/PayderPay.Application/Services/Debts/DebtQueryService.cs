using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Dtos.Debts;
using PayderPay.Application.Dtos.External;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;
using System.Net;

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

    public async Task<DebtQueryResponse> QueryAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{subscriptionId}' was not found.");

        if (subscription.Status != SubscriptionStatus.Active)
        {
            throw new BadRequestException("Debt query can only be performed for active subscriptions.");
        }

        var providerRequest = new DebtProviderQueryRequest
        {
            SubscriberNumber = subscription.SubscriberNumber
        };

        DebtProviderQueryResponse providerResponse;
        try
        {
            providerResponse = await _debtProviderClient.QueryDebtAsync(providerRequest, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            providerResponse = new DebtProviderQueryResponse
            {
                SubscriberNumber = subscription.SubscriberNumber,
                Debts = Array.Empty<DebtProviderDebtItem>()
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new BadRequestException("Debt query request is invalid.");
        }

        var queriedAtUtc = DateTime.UtcNow;
        var currentSnapshots = providerResponse.Debts
            .Select(item => new DebtQueryResult
            {
                DebtId = item.DebtId,
                SubscriptionId = subscription.Id,
                SubscriberNumber = string.IsNullOrWhiteSpace(providerResponse.SubscriberNumber)
                    ? subscription.SubscriberNumber
                    : providerResponse.SubscriberNumber,
                Amount = item.Amount,
                DueDate = item.DueDate,
                PeriodYear = item.PeriodYear,
                PeriodMonth = item.PeriodMonth,
                QueriedAtUtc = queriedAtUtc,
                ProviderRef = item.ProviderRef,
                ProviderName = string.IsNullOrWhiteSpace(item.ProviderName)
                    ? subscription.ProviderName
                    : item.ProviderName
            })
            .ToList();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _debtQueryResultRepository.SoftDeleteCurrentBySubscriptionAsync(subscription.Id, cancellationToken);

            if (currentSnapshots.Count > 0)
            {
                await _debtQueryResultRepository.AddRangeAsync(currentSnapshots, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return BuildResponse(subscription, currentSnapshots);
    }

    public async Task<DebtQueryResponse> GetCurrentAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(subscriptionId, cancellationToken);
    }

    private static DebtQueryResponse BuildResponse(Subscription subscription, IReadOnlyList<DebtQueryResult> items)
    {
        var debts = items
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.PeriodYear)
            .ThenBy(x => x.PeriodMonth)
            .Select(x => new DebtQueryHistoryItemResponse
            {
                DebtId = x.DebtId,
                SubscriptionId = x.SubscriptionId,
                SubscriberNumber = x.SubscriberNumber,
                Amount = x.Amount,
                DueDate = x.DueDate,
                PeriodYear = x.PeriodYear,
                PeriodMonth = x.PeriodMonth,
                QueriedAtUtc = x.QueriedAtUtc,
                ProviderRef = x.ProviderRef,
                ProviderName = x.ProviderName
            })
            .ToList();

        return new DebtQueryResponse
        {
            SubscriptionId = subscription.Id,
            SubscriberNumber = subscription.SubscriberNumber,
            Debts = debts
        };
    }
}
