using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        public Task<DebtProviderQueryResponse> QueryDebtAsync(DebtProviderQueryRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DebtProviderQueryResponse
            {
                Amount = 100 + request.PeriodMonth,
                DueDate = new DateOnly(request.PeriodYear, request.PeriodMonth, Math.Min(request.DueDayOfMonth, DateTime.DaysInMonth(request.PeriodYear, request.PeriodMonth))),
                PeriodYear = request.PeriodYear,
                PeriodMonth = request.PeriodMonth,
                ProviderRef = $"MOCK-{request.PeriodYear}{request.PeriodMonth:D2}"
            });
        }
    }

    private sealed class FakePaymentGatewayClient : IPaymentGatewayClient
    {
        public Task<PaymentGatewayResponse> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
        {
            if (request.PeriodMonth == 2)
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
