using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Dtos.External;
using PayderPay.Application.Dtos.Payments;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Application.Services;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;

namespace PayderPay.Application.UnitTests;

public class PaymentServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenSuccessfulPaymentAlreadyExists()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: true);
        var mainAccountRepository = new InMemoryMainAccountRepository(1000m);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtBelongsToSubscription: true);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtQueryResultId = TestData.DebtQueryResultId }));

        Assert.Equal(0, gateway.CallCount);
        Assert.Empty(paymentRepository.AddedPayments);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenDebtDoesNotBelongToSubscription()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(1000m);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtBelongsToSubscription: false);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtQueryResultId = TestData.DebtQueryResultId }));

        Assert.Equal(0, gateway.CallCount);
        Assert.Empty(paymentRepository.AddedPayments);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenInsufficientBalance()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 100m);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtBelongsToSubscription: true);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtQueryResultId = TestData.DebtQueryResultId }));

        Assert.Equal(0, gateway.CallCount);
        Assert.Empty(paymentRepository.AddedPayments);
        Assert.Equal(100m, mainAccountRepository.MainAccount.Balance);
    }

    [Fact]
    public async Task CreateAsync_ShouldDeductBalance_WhenGatewaySuccessful()
    {
        var gateway = new FakePaymentGatewayClient(success: true);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 500m);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtBelongsToSubscription: true);

        var result = await service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtQueryResultId = TestData.DebtQueryResultId });

        Assert.Equal(PaymentStatus.Successful, result.Status);
        Assert.Equal(1, gateway.CallCount);
        Assert.Single(paymentRepository.AddedPayments);
        Assert.Equal(250m, mainAccountRepository.MainAccount.Balance);
    }

    [Fact]
    public async Task CreateAsync_ShouldKeepBalance_WhenGatewayFails()
    {
        var gateway = new FakePaymentGatewayClient(success: false);
        var paymentRepository = new InMemoryPaymentRepository(hasSuccessfulPayment: false);
        var mainAccountRepository = new InMemoryMainAccountRepository(initialBalance: 500m);

        var service = BuildService(paymentRepository, mainAccountRepository, gateway, debtBelongsToSubscription: true);

        var result = await service.CreateAsync(
            TestData.SubscriptionId,
            new CreatePaymentRequest { DebtQueryResultId = TestData.DebtQueryResultId });

        Assert.Equal(PaymentStatus.Failed, result.Status);
        Assert.Equal(1, gateway.CallCount);
        Assert.Single(paymentRepository.AddedPayments);
        Assert.Equal(500m, mainAccountRepository.MainAccount.Balance);
    }

    private static PaymentService BuildService(
        InMemoryPaymentRepository paymentRepository,
        InMemoryMainAccountRepository mainAccountRepository,
        FakePaymentGatewayClient gateway,
        bool debtBelongsToSubscription)
    {
        return new PaymentService(
            new InMemorySubscriptionRepository(),
            new InMemoryDebtQueryResultRepository(debtBelongsToSubscription),
            mainAccountRepository,
            paymentRepository,
            gateway,
            new FakeUnitOfWork());
    }

    private static class TestData
    {
        public static readonly Guid SubscriptionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid AnotherSubscriptionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DebtQueryResultId = Guid.Parse("33333333-3333-3333-3333-333333333333");
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
                Status = SubscriptionStatus.Active
            });
        }

        public Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>(Array.Empty<Subscription>());

        public Task<IReadOnlyList<Subscription>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>(Array.Empty<Subscription>());

        public Task<IReadOnlyList<Subscription>> GetDueSubscriptionsAsync(DateOnly referenceDate, int leadDays, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>(Array.Empty<Subscription>());

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Update(Subscription subscription)
        {
        }

        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryDebtQueryResultRepository : IDebtQueryResultRepository
    {
        private readonly bool _belongsToSubscription;

        public InMemoryDebtQueryResultRepository(bool belongsToSubscription)
        {
            _belongsToSubscription = belongsToSubscription;
        }

        public Task AddAsync(DebtQueryResult debtQueryResult, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<DebtQueryResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id != TestData.DebtQueryResultId)
            {
                return Task.FromResult<DebtQueryResult?>(null);
            }

            var subscriptionId = _belongsToSubscription ? TestData.SubscriptionId : TestData.AnotherSubscriptionId;

            return Task.FromResult<DebtQueryResult?>(new DebtQueryResult
            {
                Id = TestData.DebtQueryResultId,
                SubscriptionId = subscriptionId,
                Amount = 250,
                PeriodYear = 2026,
                PeriodMonth = 5,
                DueDate = new DateOnly(2026, 5, 20),
                QueriedAtUtc = DateTime.UtcNow
            });
        }

        public Task<DebtQueryResult?> GetLatestBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<DebtQueryResult?>(null);

        public Task<IReadOnlyList<DebtQueryResult>> GetHistoryBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DebtQueryResult>>(Array.Empty<DebtQueryResult>());
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

        public Task<bool> HasSuccessfulPaymentForPeriodAsync(Guid subscriptionId, int periodYear, int periodMonth, CancellationToken cancellationToken = default) =>
            Task.FromResult(_hasSuccessfulPayment);

        public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            AddedPayments.Add(payment);
            return Task.CompletedTask;
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
    }
}
