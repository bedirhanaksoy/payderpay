using PayderPay.Application.Common.Exceptions;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Dtos.Auth;
using PayderPay.Application.Dtos.Customers;
using PayderPay.Domain.Entities;

namespace PayderPay.Application.Services;

public class AuthService : IAuthService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMainAccountRepository _mainAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIbanGenerator _ibanGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        ICustomerRepository customerRepository,
        IMainAccountRepository mainAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IIbanGenerator ibanGenerator,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _mainAccountRepository = mainAccountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _ibanGenerator = ibanGenerator;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (request.InitialMainAccountBalance < 0)
        {
            throw new BadRequestException("Initial main account balance cannot be negative.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _customerRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A customer with this email already exists.");
        }

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true
        };

        var mainAccount = new MainAccount
        {
            CustomerId = customer.Id,
            Iban = await _ibanGenerator.GenerateUniqueIbanAsync(cancellationToken),
            Balance = request.InitialMainAccountBalance
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _customerRepository.AddAsync(customer, cancellationToken);
            await _mainAccountRepository.AddAsync(mainAccount, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = await IssueTokensAsync(customer, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var customer = await _customerRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (customer is null || string.IsNullOrWhiteSpace(customer.PasswordHash) || !_passwordHasher.Verify(request.Password, customer.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!customer.IsActive)
        {
            throw new UnauthorizedException("Customer is inactive.");
        }

        return await IssueTokensAsync(customer, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenValue = request.RefreshToken.Trim();
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            throw new UnauthorizedException("Refresh token is required.");
        }

        var tokenHash = _jwtTokenGenerator.HashRefreshToken(tokenValue);
        var storedRefreshToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (storedRefreshToken is null || !storedRefreshToken.IsActive)
        {
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        var customer = await _customerRepository.GetByIdAsync(storedRefreshToken.CustomerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        storedRefreshToken.RevokedAtUtc = DateTime.UtcNow;
        _refreshTokenRepository.Update(storedRefreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(customer, cancellationToken);
    }

    public async Task LogoutAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
        {
            throw new UnauthorizedException("Unauthorized.");
        }

        await _refreshTokenRepository.RevokeAllForCustomerAsync(customerId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(Customer customer, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(customer);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshTokenValue();
        var refreshTokenExpiry = _jwtTokenGenerator.GetRefreshTokenExpiryUtc();

        var refreshToken = new RefreshToken
        {
            CustomerId = customer.Id,
            TokenHash = _jwtTokenGenerator.HashRefreshToken(refreshTokenValue),
            ExpiresAtUtc = refreshTokenExpiry
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshTokenValue,
            RefreshTokenExpiresAtUtc = refreshTokenExpiry,
            Customer = ToCustomerResponse(customer)
        };
    }

    private static CustomerResponse ToCustomerResponse(Customer customer)
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
