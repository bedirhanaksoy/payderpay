using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Dtos.Customers;
using PayderPay.Application.Common.Exceptions;
using PayderPay.Domain.Entities;

namespace PayderPay.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMainAccountRepository _mainAccountRepository;
    private readonly IIbanGenerator _ibanGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        ICustomerRepository customerRepository,
        IMainAccountRepository mainAccountRepository,
        IIbanGenerator ibanGenerator,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _mainAccountRepository = mainAccountRepository;
        _ibanGenerator = ibanGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (request.InitialMainAccountBalance < 0)
        {
            throw new BadRequestException("Initial main account balance cannot be negative.");
        }

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            IsActive = true
        };

        var iban = await _ibanGenerator.GenerateUniqueIbanAsync(cancellationToken);
        var mainAccount = new MainAccount
        {
            CustomerId = customer.Id,
            Iban = iban,
            Balance = request.InitialMainAccountBalance
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _mainAccountRepository.AddAsync(mainAccount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found.");

        return ToResponse(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        return customers.Select(ToResponse).ToList();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found.");

        await _customerRepository.SoftDeleteAsync(customer.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static CustomerResponse ToResponse(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            IsActive = customer.IsActive,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };
    }
}
