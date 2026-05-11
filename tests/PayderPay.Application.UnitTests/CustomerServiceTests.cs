using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Pagination;
using PayderPay.Application.Dtos.Customers;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Application.Services;
using PayderPay.Domain.Entities;

namespace PayderPay.Application.UnitTests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateCustomerAndMainAccount()
    {
        var customerRepository = new InMemoryCustomerRepository();
        var mainAccountRepository = new InMemoryMainAccountRepository();
        var ibanGenerator = new FakeIbanGenerator("TR123456789012345678901234");

        var service = new CustomerService(
            customerRepository,
            mainAccountRepository,
            ibanGenerator,
            new FakeUnitOfWork());

        var result = await service.CreateAsync(new CreateCustomerRequest
        {
            FullName = " Test User ",
            Email = " test@example.com ",
            PhoneNumber = " 5551112233 ",
            InitialMainAccountBalance = 150m
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(customerRepository.Customers);
        Assert.Single(mainAccountRepository.MainAccounts);

        var account = mainAccountRepository.MainAccounts[0];
        Assert.Equal(result.Id, account.CustomerId);
        Assert.Equal("TR123456789012345678901234", account.Iban);
        Assert.Equal(150m, account.Balance);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenInitialBalanceNegative()
    {
        var service = new CustomerService(
            new InMemoryCustomerRepository(),
            new InMemoryMainAccountRepository(),
            new FakeIbanGenerator("TR123456789012345678901234"),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateCustomerRequest
        {
            FullName = "Test User",
            Email = "test@example.com",
            PhoneNumber = "5551112233",
            InitialMainAccountBalance = -1m
        }));
    }

    private sealed class InMemoryCustomerRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = new();

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Customers.FirstOrDefault(x => x.Id == id));
        }

        public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Customers.FirstOrDefault(x =>
                string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Customers.Any(x =>
                string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Customer>>(Customers);
        }

        public Task<PagedResult<Customer>> GetAllPagedAsync(PageRequest page, CancellationToken cancellationToken = default)
        {
            var ordered = Customers.OrderBy(x => x.FullName).ToList();
            var total = ordered.Count;
            var items = ordered.Skip(page.Skip).Take(page.NormalizedPageSize).ToList();
            return Task.FromResult(PagedResult<Customer>.From(items, total, page));
        }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            Customers.Add(customer);
            return Task.CompletedTask;
        }

        public void Update(Customer customer)
        {
        }

        public Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMainAccountRepository : IMainAccountRepository
    {
        public List<MainAccount> MainAccounts { get; } = new();

        public Task<MainAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MainAccounts.FirstOrDefault(x => x.CustomerId == customerId));
        }

        public Task<bool> ExistsByIbanAsync(string iban, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MainAccounts.Any(x => x.Iban == iban));
        }

        public Task AddAsync(MainAccount mainAccount, CancellationToken cancellationToken = default)
        {
            MainAccounts.Add(mainAccount);
            return Task.CompletedTask;
        }

        public void Update(MainAccount mainAccount)
        {
        }
    }

    private sealed class FakeIbanGenerator : IIbanGenerator
    {
        private readonly string _iban;

        public FakeIbanGenerator(string iban)
        {
            _iban = iban;
        }

        public Task<string> GenerateUniqueIbanAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_iban);
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
