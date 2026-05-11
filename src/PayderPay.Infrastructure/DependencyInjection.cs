using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Settings;
using PayderPay.Infrastructure.ExternalServices;
using PayderPay.Infrastructure.Persistence;
using PayderPay.Infrastructure.Repositories;
using PayderPay.Infrastructure.Security;

namespace PayderPay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=payderpay;Username=postgres;Password=postgres";

        services.AddDbContext<PayderPayDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<ReminderJobSettings>(configuration.GetSection(ReminderJobSettings.SectionName));
        services.Configure<ExternalServiceSettings>(configuration.GetSection(ExternalServiceSettings.SectionName));

        services.AddHttpClient<IDebtProviderClient, DebtProviderClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalServiceSettings>>().Value;
            client.BaseAddress = new Uri(settings.MockApiBaseUrl);
        });

        services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalServiceSettings>>().Value;
            client.BaseAddress = new Uri(settings.MockApiBaseUrl);
        });

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IMainAccountRepository, MainAccountRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDebtQueryResultRepository, DebtQueryResultRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIbanGenerator, IbanGenerator>();

        return services;
    }
}
