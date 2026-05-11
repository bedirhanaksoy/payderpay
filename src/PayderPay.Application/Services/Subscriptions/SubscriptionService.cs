using Microsoft.Extensions.Logging;
using PayderPay.Application.Common.Pagination;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Dtos.Subscriptions;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private const int DefaultDueDayOfMonth = 1;

    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDebtQueryResultRepository _debtQueryResultRepository;
    private readonly IDebtQueryService _debtQueryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        ICustomerRepository customerRepository,
        IDebtQueryResultRepository debtQueryResultRepository,
        IDebtQueryService debtQueryService,
        IUnitOfWork unitOfWork,
        ILogger<SubscriptionService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _customerRepository = customerRepository;
        _debtQueryResultRepository = debtQueryResultRepository;
        _debtQueryService = debtQueryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Customer '{request.CustomerId}' was not found.");

        if (!customer.IsActive)
        {
            throw new BadRequestException("Subscription cannot be created for an inactive customer.");
        }

        var subscription = new Subscription
        {
            CustomerId = request.CustomerId,
            SubscriptionType = request.SubscriptionType,
            ProviderName = request.ProviderName.Trim(),
            SubscriberNumber = request.SubscriberNumber.Trim(),
            Status = SubscriptionStatus.Active,
            DueDayOfMonth = DefaultDueDayOfMonth
        };

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TryRefreshDueDayFromProviderAsync(subscription, cancellationToken);
        var currentDueDate = await GetCurrentDueDateAsync(subscription.Id, cancellationToken);

        return ToResponse(subscription, currentDueDate);
    }

    public async Task<SubscriptionResponse> UpdateAsync(Guid id, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _subscriptionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{id}' was not found.");

        existing.SubscriptionType = request.SubscriptionType;
        existing.ProviderName = request.ProviderName.Trim();
        existing.SubscriberNumber = request.SubscriberNumber.Trim();
        existing.Status = request.Status;

        _subscriptionRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TryRefreshDueDayFromProviderAsync(existing, cancellationToken);
        var currentDueDate = await GetCurrentDueDateAsync(existing.Id, cancellationToken);

        return ToResponse(existing, currentDueDate);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _subscriptionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{id}' was not found.");

        await _subscriptionRepository.SoftDeleteAsync(existing.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionResponse>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException($"Customer '{customerId}' was not found.");

        _ = customer;

        var subscriptions = await _subscriptionRepository.GetByCustomerAsync(customerId, cancellationToken);
        return await ToResponsesAsync(subscriptions, cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionRepository.GetActiveAsync(cancellationToken);
        return await ToResponsesAsync(subscriptions, cancellationToken);
    }

    public async Task<PagedResult<SubscriptionResponse>> GetByCustomerPagedAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = await GetByCustomerAsync(customerId, cancellationToken);
        return PaginationHelper.ToPaged(items, page, pageSize);
    }

    public async Task<PagedResult<SubscriptionResponse>> GetActivePagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = await GetActiveAsync(cancellationToken);
        return PaginationHelper.ToPaged(items, page, pageSize);
    }

    private async Task<IReadOnlyList<SubscriptionResponse>> ToResponsesAsync(
        IReadOnlyList<Subscription> subscriptions,
        CancellationToken cancellationToken)
    {
        var responses = new List<SubscriptionResponse>(subscriptions.Count);

        foreach (var subscription in subscriptions)
        {
            var currentDueDate = await GetCurrentDueDateAsync(subscription.Id, cancellationToken);
            responses.Add(ToResponse(subscription, currentDueDate));
        }

        return responses;
    }

    private async Task<DateOnly?> GetCurrentDueDateAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var debts = await _debtQueryResultRepository.GetCurrentBySubscriptionAsync(subscriptionId, cancellationToken);
        return debts.Count == 0 ? null : debts[0].DueDate;
    }

    private async Task TryRefreshDueDayFromProviderAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _debtQueryService.QueryAsync(subscription.Id, cancellationToken);
            var nearestDueDate = response.Debts
                .OrderBy(x => x.DueDate)
                .Select(x => (DateOnly?)x.DueDate)
                .FirstOrDefault();

            if (!nearestDueDate.HasValue)
            {
                return;
            }

            var nextDueDay = nearestDueDate.Value.Day;
            if (subscription.DueDayOfMonth == nextDueDay)
            {
                return;
            }

            subscription.DueDayOfMonth = nextDueDay;
            _subscriptionRepository.Update(subscription);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Debt provider timed out while refreshing due date for subscription {SubscriptionId}.",
                subscription.Id);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Debt provider request failed while refreshing due date for subscription {SubscriptionId}.",
                subscription.Id);
        }
    }

    private static SubscriptionResponse ToResponse(Subscription subscription, DateOnly? currentDueDate)
    {
        return new SubscriptionResponse
        {
            Id = subscription.Id,
            CustomerId = subscription.CustomerId,
            SubscriptionType = subscription.SubscriptionType,
            ProviderName = subscription.ProviderName,
            SubscriberNumber = subscription.SubscriberNumber,
            Status = subscription.Status,
            DueDayOfMonth = subscription.DueDayOfMonth,
            CurrentDueDate = currentDueDate,
            CreatedAtUtc = subscription.CreatedAtUtc,
            UpdatedAtUtc = subscription.UpdatedAtUtc
        };
    }
}
