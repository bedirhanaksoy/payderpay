using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Dtos.Subscriptions;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
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
            DueDayOfMonth = request.DueDayOfMonth
        };

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(subscription);
    }

    public async Task<SubscriptionResponse> UpdateAsync(Guid id, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _subscriptionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Subscription '{id}' was not found.");

        existing.SubscriptionType = request.SubscriptionType;
        existing.ProviderName = request.ProviderName.Trim();
        existing.SubscriberNumber = request.SubscriberNumber.Trim();
        existing.Status = request.Status;
        existing.DueDayOfMonth = request.DueDayOfMonth;

        _subscriptionRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(existing);
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
        return subscriptions.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<SubscriptionResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionRepository.GetActiveAsync(cancellationToken);
        return subscriptions.Select(ToResponse).ToList();
    }

    private static SubscriptionResponse ToResponse(Subscription subscription)
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
            CreatedAtUtc = subscription.CreatedAtUtc,
            UpdatedAtUtc = subscription.UpdatedAtUtc
        };
    }
}
