using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Notifications;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Settings;
using PayderPay.Infrastructure.BackgroundJobs;
using PayderPay.Infrastructure.ExternalServices;
using PayderPay.Infrastructure.Notifications;
using PayderPay.Infrastructure.Persistence;
using PayderPay.Infrastructure.Repositories;
using PayderPay.Infrastructure.Security;

namespace PayderPay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5157;Database=payderpay_dev;Username=postgres;Password=postgres";

        services.AddDbContext<PayderPayDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<ReminderJobSettings>(configuration.GetSection(ReminderJobSettings.SectionName));
        services.Configure<ExternalServiceSettings>(configuration.GetSection(ExternalServiceSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));

        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();
        if (redisSettings.Enabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisSettings.ConnectionString;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddHangfire(config => config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        services.AddHttpClient<IDebtProviderClient, DebtProviderClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalServiceSettings>>().Value;
            client.BaseAddress = EnsureTrailingSlashUri(settings.MockApiBaseUrl);
        });

        services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalServiceSettings>>().Value;
            client.BaseAddress = EnsureTrailingSlashUri(settings.MockApiBaseUrl);
        });

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IMainAccountRepository, MainAccountRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<INotificationQueueRepository, NotificationQueueRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIbanGenerator, IbanGenerator>();
        services.AddSingleton<IRedisCacheStore, RedisCacheStore>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IEmailNotificationService, SmtpEmailNotificationService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddTransient<InvoiceSyncJob>();
        services.AddTransient<NotificationDeliveryJob>();

        services.AddJwtBearerAuthentication(configuration);
        services.AddAuthorization();

        return services;
    }

    public static IApplicationBuilder UseInfrastructureJobs(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<ReminderJobSettings>>().Value;

        if (!settings.Enabled)
        {
            return app;
        }

        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var options = new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        };

        recurringJobs.AddOrUpdate<InvoiceSyncJob>(
            "invoice-sync-job",
            job => job.ExecuteAsync(),
            settings.InvoiceSyncCron,
            options);

        recurringJobs.AddOrUpdate<NotificationDeliveryJob>(
            "notification-delivery-job",
            job => job.ExecuteAsync(),
            settings.NotificationDeliveryCron,
            options);

        return app;
    }

    private static void AddJwtBearerAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt settings are missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateCustomerIsActiveAsync
                };
            });
    }

    private static async Task ValidateCustomerIsActiveAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(subject, out var customerId))
        {
            context.Fail("Invalid customer claim.");
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<PayderPayDbContext>();
        var customer = await dbContext.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId, context.HttpContext.RequestAborted);

        if (customer is null || !customer.IsActive || customer.IsDeleted)
        {
            context.Fail("Customer account is inactive.");
        }
    }

    private static Uri EnsureTrailingSlashUri(string rawBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(rawBaseUrl))
        {
            throw new InvalidOperationException("ExternalServices:MockApiBaseUrl is required.");
        }

        var normalized = rawBaseUrl.Trim();
        if (!normalized.EndsWith('/'))
        {
            normalized += "/";
        }

        return new Uri(normalized, UriKind.Absolute);
    }
}
