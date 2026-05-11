using Microsoft.Extensions.DependencyInjection;
using PayderPay.Application.Common.Interfaces.Validation;
using PayderPay.Application.Services;
using PayderPay.Application.Validators.Auth;
using PayderPay.Application.Validators.Customers;
using PayderPay.Application.Validators.Debts;
using PayderPay.Application.Validators.Payments;
using PayderPay.Application.Validators.Subscriptions;

namespace PayderPay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IMainAccountService, MainAccountService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IDebtQueryService, DebtQueryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISummaryService, SummaryService>();

        services.AddScoped<IRequestValidator, RegisterRequestValidator>();
        services.AddScoped<IRequestValidator, LoginRequestValidator>();
        services.AddScoped<IRequestValidator, RefreshTokenRequestValidator>();
        services.AddScoped<IRequestValidator, CreateCustomerRequestValidator>();
        services.AddScoped<IRequestValidator, CreateSubscriptionRequestValidator>();
        services.AddScoped<IRequestValidator, UpdateSubscriptionRequestValidator>();
        services.AddScoped<IRequestValidator, DebtQueryRequestValidator>();
        services.AddScoped<IRequestValidator, CreatePaymentRequestValidator>();

        return services;
    }
}
