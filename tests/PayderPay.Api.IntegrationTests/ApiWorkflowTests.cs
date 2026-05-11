using System.Net;
using System.Net.Http.Json;
using PayderPay.Application.Dtos.Customers;
using PayderPay.Application.Dtos.Debts;
using PayderPay.Application.Dtos.MainAccounts;
using PayderPay.Application.Dtos.Payments;
using PayderPay.Application.Dtos.Subscriptions;
using PayderPay.Application.Dtos.Summaries;
using PayderPay.Domain.Enums;

namespace PayderPay.Api.IntegrationTests;

public class ApiWorkflowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiWorkflowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CustomerSubscriptionDebtPaymentFlow_ShouldWork_WithSuccessAndFailureCases()
    {
        var customer = await CreateCustomerAsync(initialBalance: 1000m);
        var subscription = await CreateSubscriptionAsync(customer.Id);

        var debtForFailedPayment = await QueryDebtAsync(subscription.Id, 2026, 2);
        var failedPaymentResponse = await _client.PostAsJsonAsync(
            $"/api/subscriptions/{subscription.Id}/payments",
            new CreatePaymentRequest { DebtQueryResultId = debtForFailedPayment.Id });

        Assert.Equal(HttpStatusCode.OK, failedPaymentResponse.StatusCode);
        var failedPayment = await failedPaymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(failedPayment);
        Assert.Equal(PaymentStatus.Failed, failedPayment!.Status);

        var debtForSuccess = await QueryDebtAsync(subscription.Id, 2026, 3);
        var successPaymentResponse = await _client.PostAsJsonAsync(
            $"/api/subscriptions/{subscription.Id}/payments",
            new CreatePaymentRequest { DebtQueryResultId = debtForSuccess.Id });

        Assert.Equal(HttpStatusCode.OK, successPaymentResponse.StatusCode);
        var successPayment = await successPaymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(successPayment);
        Assert.Equal(PaymentStatus.Successful, successPayment!.Status);

        var duplicateSuccessResponse = await _client.PostAsJsonAsync(
            $"/api/subscriptions/{subscription.Id}/payments",
            new CreatePaymentRequest { DebtQueryResultId = debtForSuccess.Id });

        Assert.Equal(HttpStatusCode.Conflict, duplicateSuccessResponse.StatusCode);

        var dashboard = await _client.GetFromJsonAsync<DashboardSummaryResponse>(
            $"/api/customers/{customer.Id}/dashboard?year=2026&month=3");

        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard!.ActiveSubscriptionCount);
        Assert.Equal(0, dashboard.UnpaidThisMonthCount);

        var unpaid = await _client.GetFromJsonAsync<List<UnpaidSubscriptionResponse>>(
            $"/api/subscriptions/unpaid?customerId={customer.Id}&year=2026&month=3");

        Assert.NotNull(unpaid);
        Assert.Empty(unpaid!);
    }

    [Fact]
    public async Task GetMainAccount_ShouldReturnUniqueIbanAndBalance()
    {
        var customer = await CreateCustomerAsync(initialBalance: 375m);

        var account = await _client.GetFromJsonAsync<MainAccountResponse>(
            $"/api/customers/{customer.Id}/main-account");

        Assert.NotNull(account);
        Assert.Equal(customer.Id, account!.CustomerId);
        Assert.Equal(375m, account.Balance);
        Assert.StartsWith("TR", account.Iban);
    }

    [Fact]
    public async Task Payment_ShouldReturnConflict_WhenMainAccountBalanceInsufficient()
    {
        var customer = await CreateCustomerAsync(initialBalance: 50m);
        var subscription = await CreateSubscriptionAsync(customer.Id);
        var debt = await QueryDebtAsync(subscription.Id, 2026, 4);

        var paymentResponse = await _client.PostAsJsonAsync(
            $"/api/subscriptions/{subscription.Id}/payments",
            new CreatePaymentRequest { DebtQueryResultId = debt.Id });

        Assert.Equal(HttpStatusCode.Conflict, paymentResponse.StatusCode);

        var paymentHistory = await _client.GetFromJsonAsync<List<PaymentHistoryItemResponse>>(
            $"/api/subscriptions/{subscription.Id}/payments");

        Assert.NotNull(paymentHistory);
        Assert.Empty(paymentHistory!);
    }

    [Fact]
    public async Task SoftDeletedCustomer_ShouldNotAppearInCustomerList()
    {
        var customer = await CreateCustomerAsync(initialBalance: 100m);

        var deleteResponse = await _client.DeleteAsync($"/api/customers/{customer.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers");
        Assert.NotNull(listResponse);
        Assert.DoesNotContain(listResponse!, x => x.Id == customer.Id);
    }

    private async Task<CustomerResponse> CreateCustomerAsync(decimal initialBalance)
    {
        var response = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest
        {
            FullName = "Test User",
            Email = $"test-{Guid.NewGuid():N}@mail.com",
            PhoneNumber = "5551112233",
            InitialMainAccountBalance = initialBalance
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        return customer!;
    }

    private async Task<SubscriptionResponse> CreateSubscriptionAsync(Guid customerId)
    {
        var response = await _client.PostAsJsonAsync("/api/subscriptions", new CreateSubscriptionRequest
        {
            CustomerId = customerId,
            SubscriptionType = SubscriptionType.Electricity,
            ProviderName = "Provider A",
            SubscriberNumber = $"SUB-{Guid.NewGuid():N}",
            DueDayOfMonth = 20
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var subscription = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(subscription);
        return subscription!;
    }

    private async Task<DebtQueryHistoryItemResponse> QueryDebtAsync(Guid subscriptionId, int year, int month)
    {
        var queryResponse = await _client.PostAsJsonAsync(
            $"/api/subscriptions/{subscriptionId}/debt-queries",
            new DebtQueryRequest { PeriodYear = year, PeriodMonth = month });

        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);

        var historyResponse = await _client.GetAsync($"/api/subscriptions/{subscriptionId}/debt-queries");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var history = await historyResponse.Content.ReadFromJsonAsync<List<DebtQueryHistoryItemResponse>>();
        Assert.NotNull(history);

        var item = history!
            .FirstOrDefault(x => x.PeriodYear == year && x.PeriodMonth == month);

        Assert.NotNull(item);
        return item!;
    }
}
