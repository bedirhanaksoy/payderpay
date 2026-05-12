using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Helpers;
using PayderPay.Application.Common.Settings;
using PayderPay.Application.Dtos.Debts;
using PayderPay.Application.Dtos.External;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;
using System.Net;
using Microsoft.Extensions.Options;

namespace PayderPay.Application.Services;

public class DebtQueryService : IDebtQueryService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly IDebtProviderClient _debtProviderClient;
    private readonly IRedisCacheStore _redisCacheStore;
    private readonly TimeSpan _debtCacheTtl;
    private readonly IUnitOfWork _unitOfWork;

    public DebtQueryService(
        ISubscriptionRepository subscriptionRepository,
        IDebtRepository debtRepository,
        IDebtProviderClient debtProviderClient,
        IRedisCacheStore redisCacheStore,
        IOptions<RedisSettings> redisSettings,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _debtRepository = debtRepository;
        _debtProviderClient = debtProviderClient;
        _redisCacheStore = redisCacheStore;
        var ttl = redisSettings.Value.DebtTtlSeconds;
        _debtCacheTtl = TimeSpan.FromSeconds(ttl > 0 ? ttl : 60);
        _unitOfWork = unitOfWork;
    }

    public async Task<DebtQueryResponse> QueryAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await QueryInternalAsync(subscriptionId, useCache: true, cancellationToken);
    }

    public async Task<DebtQueryResponse> QueryLiveAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await QueryInternalAsync(subscriptionId, useCache: false, cancellationToken);
    }

    public async Task<DebtQueryResponse> GetCurrentAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(subscriptionId, cancellationToken);
    }

    private async Task<DebtQueryResponse> QueryInternalAsync(Guid subscriptionId, bool useCache, CancellationToken cancellationToken)
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

        var providerResponse = await GetProviderResponseAsync(providerRequest, useCache, cancellationToken);

        var queriedAtUtc = DateTime.UtcNow;
        var currentSnapshots = providerResponse.Debts
            .Select(item => new Debt
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
            await _debtRepository.SoftDeleteCurrentBySubscriptionAsync(subscription.Id, cancellationToken);

            if (currentSnapshots.Count > 0)
            {
                await _debtRepository.AddRangeAsync(currentSnapshots, cancellationToken);
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

    private async Task<DebtProviderQueryResponse> GetProviderResponseAsync(
        DebtProviderQueryRequest request,
        bool useCache,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyFactory.DebtBySubscriber(request.SubscriberNumber);
        if (useCache)
        {
            var cached = await _redisCacheStore.GetAsync<DebtProviderQueryResponse>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return NormalizeProviderResponse(cached, request.SubscriberNumber);
            }
        }

        DebtProviderQueryResponse providerResponse;
        try
        {
            providerResponse = await _debtProviderClient.QueryDebtAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            providerResponse = new DebtProviderQueryResponse
            {
                SubscriberNumber = request.SubscriberNumber,
                Debts = Array.Empty<DebtProviderDebtItem>()
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new BadRequestException("Debt query request is invalid.");
        }

        providerResponse = NormalizeProviderResponse(providerResponse, request.SubscriberNumber);

        if (useCache)
        {
            await _redisCacheStore.SetAsync(cacheKey, providerResponse, _debtCacheTtl, cancellationToken);
        }

        return providerResponse;
    }

    private static DebtProviderQueryResponse NormalizeProviderResponse(DebtProviderQueryResponse response, string fallbackSubscriberNumber)
    {
        return new DebtProviderQueryResponse
        {
            SubscriberNumber = string.IsNullOrWhiteSpace(response.SubscriberNumber)
                ? fallbackSubscriberNumber
                : response.SubscriberNumber,
            Debts = response.Debts ?? Array.Empty<DebtProviderDebtItem>()
        };
    }

    private static DebtQueryResponse BuildResponse(Subscription subscription, IReadOnlyList<Debt> items)
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
