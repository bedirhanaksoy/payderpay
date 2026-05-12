using Microsoft.Extensions.Logging.Abstractions;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Notifications;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Pagination;
using PayderPay.Application.Dtos.Debts;
using PayderPay.Application.Dtos.External;
using PayderPay.Application.Dtos.Payments;
using PayderPay.Application.Services;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.UnitTests;

public class PaymentServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenSuccessfulPaymentAlreadyExistsForDebt()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: true);
        var mainAccountRepository = new InMemoryMainAccountRepository(1000m);
        var debtQueryService = new FakeDebtQueryService(TestData.SubscriptionId, [CreateLiveDebt(250m)]);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtQueryService, debtBelongsToSubscription: true);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtId = TestData.DebtId }));

        Assert.Equal(0, gateway.CallCount);
        Assert.Empty(paymentRepository.AddedPayments);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenDebtDoesNotBelongToSubscription()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(1000m);
        var debtQueryService = new FakeDebtQueryService(TestData.SubscriptionId, [CreateLiveDebt(250m)]);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtQueryService, debtBelongsToSubscription: false);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtId = TestData.DebtId }));

        Assert.Equal(0, gateway.CallCount);
        Assert.Empty(paymentRepository.AddedPayments);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenInsufficientBalance()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 100m);
        var debtQueryService = new FakeDebtQueryService(TestData.SubscriptionId, [CreateLiveDebt(250m)]);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtQueryService, debtBelongsToSubscription: true);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtId = TestData.DebtId }));

        Assert.Equal(0, gateway.CallCount);
        Assert.Empty(paymentRepository.AddedPayments);
        Assert.Equal(100m, mainAccountRepository.MainAccount.Balance);
    }

    [Fact]
    public async Task CreateAsync_ShouldDeductBalance_AndCloseDebtSnapshot_WhenGatewaySuccessful()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 500m);
        var debtRepository = new InMemoryDebtRepository(debtBelongsToSubscription: true);
        var debtQueryService = new FakeDebtQueryService(TestData.SubscriptionId, [CreateLiveDebt(250m)]);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtQueryService, debtRepository: debtRepository);

        var result = await service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtId = TestData.DebtId });

        Assert.Equal(PaymentStatus.Successful, result.Status);
        Assert.Equal(TestData.DebtId, result.DebtId);
        Assert.Equal(1, gateway.CallCount);
        Assert.Single(paymentRepository.AddedPayments);
        Assert.Equal(TestData.DebtId, paymentRepository.AddedPayments[0].DebtId);
        Assert.Equal(250m, mainAccountRepository.MainAccount.Balance);

        Assert.NotNull(debtRepository.UpdatedDebt);
        Assert.True(debtRepository.UpdatedDebt!.IsDeleted);
        Assert.NotNull(debtRepository.UpdatedDebt.DeletedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_ShouldKeepBalance_AndDebtOpen_WhenGatewayFails()
    {
        var gateway = new FakePaymentGatewayClient(success: false);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 500m);
        var debtRepository = new InMemoryDebtRepository(debtBelongsToSubscription: true);
        var debtQueryService = new FakeDebtQueryService(TestData.SubscriptionId, [CreateLiveDebt(250m)]);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtQueryService, debtRepository: debtRepository);

        var result = await service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtId = TestData.DebtId });

        Assert.Equal(PaymentStatus.Failed, result.Status);
        Assert.Equal(1, gateway.CallCount);
        Assert.Single(paymentRepository.AddedPayments);
        Assert.Equal(500m, mainAccountRepository.MainAccount.Balance);
        Assert.Null(debtRepository.UpdatedDebt);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenDebtMissingInLiveProviderQuery()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 500m);
        var debtQueryService = new FakeDebtQueryService(TestData.SubscriptionId, Array.Empty<DebtQueryHistoryItemResponse>());

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtQueryService, debtBelongsToSubscription: true);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtId = TestData.DebtId }));

        Assert.Equal(0, gateway.CallCount);
        Assert.Empty(paymentRepository.AddedPayments);
        Assert.Equal(500m, mainAccountRepository.MainAccount.Balance);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenDebtAmountChangedInLiveProviderQuery()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 500m);
        var debtQueryService = new FakeDebtQueryService(TestData.SubscriptionId, [CreateLiveDebt(275m)]);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtQueryService, debtBelongsToSubscription: true);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtId = TestData.DebtId }));

        Assert.Equal(0, gateway.CallCount);
        Assert.Empty(paymentRepository.AddedPayments);
        Assert.Equal(500m, mainAccountRepository.MainAccount.Balance);
    }

    [Fact]
    public async Task CreateAsync_ShouldUseLiveDebtQueryForRevalidation()
    {
        var gateway = new FakePaymentGatewayClient(success: false);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 500m);
        var debtRepository = new InMemoryDebtRepository(debtBelongsToSubscription: true);
        var debtQueryService = new FakeDebtQueryService(TestData.SubscriptionId, [CreateLiveDebt(250m)]);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtQueryService, debtRepository: debtRepository);

        _ = await service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtId = TestData.DebtId });

        Assert.Equal(1, debtQueryService.QueryLiveCallCount);
        Assert.Equal(0, debtQueryService.QueryCallCount);
    }

    private static PaymentService BuildService(
        InMemoryPaymentRepository paymentRepository,
        InMemoryMainAccountRepository mainAccountRepository,
        FakePaymentGatewayClient gateway,
        FakeDebtQueryService debtQueryService,
        bool? debtBelongsToSubscription = null,
        InMemoryDebtRepository? debtRepository = null)
    {
        debtRepository ??= new InMemoryDebtRepository(debtBelongsToSubscription ?? true);

        return new PaymentService(
            new InMemorySubscriptionRepository(),
            debtQueryService,
            debtRepository,
            mainAccountRepository,
            paymentRepository,
            new InMemoryNotificationLogRepository(),
            gateway,
            new FakeEmailNotificationService(),
            new InMemoryRedisCacheStore(),
            new FakeUnitOfWork(),
            NullLogger<PaymentService>.Instance);
    }

    private static DebtQueryHistoryItemResponse CreateLiveDebt(decimal amount)
    {
        return new DebtQueryHistoryItemResponse
        {
            DebtId = TestData.DebtId,
            SubscriptionId = TestData.SubscriptionId,
            SubscriberNumber = "SUB-1",
            Amount = amount,
            DueDate = new DateOnly(2026, 5, 20),
            PeriodYear = 2026,
            PeriodMonth = 5,
            QueriedAtUtc = DateTime.UtcNow,
            ProviderRef = "MOCK-202605",
            ProviderName = "Provider"
        };
    }

    private static class TestData
    {
        public static readonly Guid SubscriptionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DebtId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid CustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    }

    private sealed class InMemorySubscriptionRepository : ISubscriptionRepository
    {
        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id != TestData.SubscriptionId)
            {
                return Task.FromResult<Subscription?>(null);
            }

            return Task.FromResult<Subscription?>(new Subscription
            {
                Id = TestData.SubscriptionId,
                CustomerId = TestData.CustomerId,
                SubscriptionType = SubscriptionType.Electricity,
                ProviderName = "Provider",
                SubscriberNumber = "SUB-1",
                DueDayOfMonth = 10,
                Status = SubscriptionStatus.Active,
                Customer = new Customer
                {
                    Id = TestData.CustomerId,
                    FullName = "Test User",
                    Email = "test@example.com",
                    PhoneNumber = "5551112233",
                    IsActive = true
                }
            });
        }

        public Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>(Array.Empty<Subscription>());

        public Task<IReadOnlyList<Subscription>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>(Array.Empty<Subscription>());

        public Task<PagedResult<Subscription>> GetByCustomerPagedAsync(Guid customerId, PageRequest page, CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<Subscription>.Empty(page));

        public Task<PagedResult<Subscription>> GetActivePagedAsync(PageRequest page, CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<Subscription>.Empty(page));

        public Task<IReadOnlyList<Subscription>> GetDueSubscriptionsAsync(DateOnly referenceDate, int leadDays, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>(Array.Empty<Subscription>());

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Update(Subscription subscription)
        {
        }

        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryDebtRepository : IDebtRepository
    {
        private readonly bool _debtBelongsToSubscription;
        private readonly Debt _debt;

        public InMemoryDebtRepository(bool debtBelongsToSubscription)
        {
            _debtBelongsToSubscription = debtBelongsToSubscription;
            _debt = new Debt
            {
                DebtId = TestData.DebtId,
                SubscriptionId = debtBelongsToSubscription ? TestData.SubscriptionId : Guid.NewGuid(),
                SubscriberNumber = "SUB-1",
                Amount = 250,
                PeriodYear = 2026,
                PeriodMonth = 5,
                DueDate = new DateOnly(2026, 5, 20),
                QueriedAtUtc = DateTime.UtcNow,
                ProviderName = "Provider",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        public Debt? UpdatedDebt { get; private set; }

        public Task AddAsync(Debt debtQueryResult, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddRangeAsync(IReadOnlyList<Debt> debtQueryResults, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Debt?> GetCurrentBySubscriptionAndDebtIdAsync(Guid subscriptionId, Guid debtId, CancellationToken cancellationToken = default)
        {
            if (!_debtBelongsToSubscription || debtId != TestData.DebtId || subscriptionId != TestData.SubscriptionId || _debt.IsDeleted)
            {
                return Task.FromResult<Debt?>(null);
            }

            return Task.FromResult<Debt?>(_debt);
        }

        public Task<IReadOnlyList<Debt>> GetCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            if (_debtBelongsToSubscription && subscriptionId == TestData.SubscriptionId && !_debt.IsDeleted)
            {
                return Task.FromResult<IReadOnlyList<Debt>>([_debt]);
            }

            return Task.FromResult<IReadOnlyList<Debt>>(Array.Empty<Debt>());
        }

        public Task SoftDeleteCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Update(Debt debtQueryResult)
        {
            UpdatedDebt = debtQueryResult;
        }
    }

    private sealed class FakeDebtQueryService : IDebtQueryService
    {
        private readonly Guid _subscriptionId;
        private readonly IReadOnlyList<DebtQueryHistoryItemResponse> _debts;

        public FakeDebtQueryService(Guid subscriptionId, IReadOnlyList<DebtQueryHistoryItemResponse> debts)
        {
            _subscriptionId = subscriptionId;
            _debts = debts;
        }

        public int QueryCallCount { get; private set; }
        public int QueryLiveCallCount { get; private set; }

        public Task<DebtQueryResponse> QueryAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            QueryCallCount++;

            if (subscriptionId != _subscriptionId)
            {
                return Task.FromResult(new DebtQueryResponse
                {
                    SubscriptionId = subscriptionId,
                    SubscriberNumber = string.Empty,
                    Debts = Array.Empty<DebtQueryHistoryItemResponse>()
                });
            }

            return Task.FromResult(new DebtQueryResponse
            {
                SubscriptionId = _subscriptionId,
                SubscriberNumber = "SUB-1",
                Debts = _debts
            });
        }

        public Task<DebtQueryResponse> GetCurrentAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            return QueryAsync(subscriptionId, cancellationToken);
        }

        public Task<DebtQueryResponse> QueryLiveAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            QueryLiveCallCount++;

            if (subscriptionId != _subscriptionId)
            {
                return Task.FromResult(new DebtQueryResponse
                {
                    SubscriptionId = subscriptionId,
                    SubscriberNumber = string.Empty,
                    Debts = Array.Empty<DebtQueryHistoryItemResponse>()
                });
            }

            return Task.FromResult(new DebtQueryResponse
            {
                SubscriptionId = _subscriptionId,
                SubscriberNumber = "SUB-1",
                Debts = _debts
            });
        }
    }

    private sealed class InMemoryMainAccountRepository : IMainAccountRepository
    {
        public InMemoryMainAccountRepository(decimal initialBalance)
        {
            MainAccount = new MainAccount
            {
                CustomerId = TestData.CustomerId,
                Iban = "TR000000000000000000000001",
                Balance = initialBalance
            };
        }

        public MainAccount MainAccount { get; }

        public Task<MainAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(customerId == TestData.CustomerId ? MainAccount : null);
        }

        public Task<bool> ExistsByIbanAsync(string iban, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MainAccount.Iban == iban);
        }

        public Task AddAsync(MainAccount mainAccount, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Update(MainAccount mainAccount)
        {
        }
    }

    private sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly bool _hasSuccessfulPayment;

        public InMemoryPaymentRepository(bool hasSuccessfulPayment)
        {
            _hasSuccessfulPayment = hasSuccessfulPayment;
        }

        public List<Payment> AddedPayments { get; } = new();

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Payment?>(null);

        public Task<IReadOnlyList<Payment>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Payment>>(Array.Empty<Payment>());

        public Task<IReadOnlyList<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Payment>>(Array.Empty<Payment>());

        public Task<PagedResult<Payment>> GetBySubscriptionPagedAsync(Guid subscriptionId, PageRequest page, CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<Payment>.Empty(page));

        public Task<PagedResult<Payment>> GetByCustomerPagedAsync(Guid customerId, PageRequest page, CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<Payment>.Empty(page));

        public Task<bool> HasSuccessfulPaymentForPeriodAsync(Guid subscriptionId, int periodYear, int periodMonth, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasSuccessfulPaymentForDebtIdAsync(Guid debtId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_hasSuccessfulPayment);

        public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            AddedPayments.Add(payment);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRedisCacheStore : IRedisCacheStore
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNotificationLogRepository : INotificationLogRepository
    {
        public Task AddAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<NotificationLog>> GetBySubscriptionPeriodAsync(
            Guid subscriptionId,
            int periodYear,
            int periodMonth,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<NotificationLog>>(Array.Empty<NotificationLog>());
        }

        public Task<bool> HasSentReminderForPeriodAsync(
            Guid subscriptionId,
            int periodYear,
            int periodMonth,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class FakeEmailNotificationService : IEmailNotificationService
    {
        public Task<EmailDispatchResult> SendUpcomingPaymentReminderAsync(
            UpcomingPaymentReminderRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EmailDispatchResult(
                Sent: true,
                Subject: "Reminder",
                Body: "Reminder body",
                SentAtUtc: DateTime.UtcNow,
                FailureReason: null));
        }

        public Task<EmailDispatchResult> SendPaymentReceiptAsync(
            PaymentReceiptRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EmailDispatchResult(
                Sent: true,
                Subject: "Receipt",
                Body: "Receipt body",
                SentAtUtc: DateTime.UtcNow,
                FailureReason: null));
        }

        public Task<EmailDispatchResult> SendInvoiceDueReminderAsync(
            InvoiceDueReminderRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EmailDispatchResult(
                Sent: true,
                Subject: "Invoice due",
                Body: "Invoice due body",
                SentAtUtc: DateTime.UtcNow,
                FailureReason: null));
        }
    }

    private sealed class FakePaymentGatewayClient : IPaymentGatewayClient
    {
        private readonly bool _success;

        public FakePaymentGatewayClient(bool success)
        {
            _success = success;
        }

        public int CallCount { get; private set; }

        public Task<PaymentGatewayResponse> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;

            var response = new PaymentGatewayResponse
            {
                IsSuccessful = _success,
                ExternalTransactionId = _success ? "TX-123" : null,
                FailureReason = _success ? null : "Mock fail"
            };

            return Task.FromResult(response);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<bool> TryAcquireAdvisoryLockAsync(long key, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task ReleaseAdvisoryLockAsync(long key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
