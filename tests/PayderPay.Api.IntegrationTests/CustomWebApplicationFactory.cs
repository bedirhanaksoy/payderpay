using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using PayderPay.Application.Common.Interfaces.External;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Dtos.External;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Api.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"payderpay-integration-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure the API can resolve Jwt settings even when host config supplies none.
        builder.UseSetting("Jwt:Issuer", "PayderPay.Test");
        builder.UseSetting("Jwt:Audience", "PayderPay.Test.Clients");
        builder.UseSetting("Jwt:SigningKey", "integration-test-only-signing-key-32+chars-long-abcdefghijklmnop");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "7");

        builder.ConfigureServices(services =>
        {
            RemoveDbContextRegistrations(services);
            RemoveService<IDebtProviderClient>(services);
            RemoveService<IPaymentGatewayClient>(services);

            services.AddDbContext<PayderPayDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            services.AddSingleton<IDebtProviderClient, FakeDebtProviderClient>();
            services.AddSingleton<IPaymentGatewayClient, FakePaymentGatewayClient>();

            // Swap JwtBearer for the test scheme so controllers decorated with
            // [Authorize] accept the legacy un-authenticated workflow tests.
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        services.RemoveAll(typeof(PayderPayDbContext));
        services.RemoveAll(typeof(DbContextOptions));
        services.RemoveAll(typeof(DbContextOptions<PayderPayDbContext>));
        services.RemoveAll(typeof(IDbContextOptionsConfiguration<PayderPayDbContext>));
    }

    private static void RemoveService<TService>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private sealed class FakeDebtProviderClient : IDebtProviderClient
    {
        private static readonly ConcurrentDictionary<string, int> QueryCountBySubscriber = new();

        public Task<DebtProviderQueryResponse> QueryDebtAsync(DebtProviderQueryRequest request, CancellationToken cancellationToken = default)
        {
            var subscriberNumber = request.SubscriberNumber.Trim();
            if (string.IsNullOrWhiteSpace(subscriberNumber))
            {
                return Task.FromResult(new DebtProviderQueryResponse
                {
                    SubscriberNumber = string.Empty,
                    Debts = Array.Empty<DebtProviderDebtItem>()
                });
            }

            var debtFail = new DebtProviderDebtItem
            {
                DebtId = CreateDeterministicDebtId(subscriberNumber, 1),
                Amount = 102m,
                DueDate = new DateOnly(2026, 2, 20),
                PeriodYear = 2026,
                PeriodMonth = 2,
                ProviderRef = $"MOCK-{subscriberNumber}-202602",
                ProviderName = "Provider A"
            };

            var debtSuccess = new DebtProviderDebtItem
            {
                DebtId = CreateDeterministicDebtId(subscriberNumber, 2),
                Amount = ResolveSuccessDebtAmount(subscriberNumber),
                DueDate = new DateOnly(2026, 3, 20),
                PeriodYear = 2026,
                PeriodMonth = 3,
                ProviderRef = $"MOCK-{subscriberNumber}-202603",
                ProviderName = "Provider A"
            };

            return Task.FromResult(new DebtProviderQueryResponse
            {
                SubscriberNumber = subscriberNumber,
                Debts = [debtFail, debtSuccess]
            });
        }

        private static Guid CreateDeterministicDebtId(string subscriberNumber, int index)
        {
            var input = Encoding.UTF8.GetBytes($"{subscriberNumber}:{index}");
            var hash = MD5.HashData(input);
            return new Guid(hash);
        }

        private static decimal ResolveSuccessDebtAmount(string subscriberNumber)
        {
            var count = QueryCountBySubscriber.AddOrUpdate(subscriberNumber, 1, (_, current) => current + 1);

            if (subscriberNumber.StartsWith("CHANGED-", StringComparison.OrdinalIgnoreCase) && count >= 2)
            {
                return 130m;
            }

            return 103m;
        }
    }

    private sealed class FakePaymentGatewayClient : IPaymentGatewayClient
    {
        public Task<PaymentGatewayResponse> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Amount == 102m)
            {
                return Task.FromResult(new PaymentGatewayResponse
                {
                    IsSuccessful = false,
                    FailureReason = "Forced fail for test"
                });
            }

            return Task.FromResult(new PaymentGatewayResponse
            {
                IsSuccessful = true,
                ExternalTransactionId = $"TX-{Guid.NewGuid():N}"
            });
        }
    }
}
