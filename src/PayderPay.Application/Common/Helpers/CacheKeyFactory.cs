namespace PayderPay.Application.Common.Helpers;

public static class CacheKeyFactory
{
    public static string DebtBySubscriber(string subscriberNumber)
        => $"debt:subscriber:{Normalize(subscriberNumber)}";

    public static string DashboardSummary(Guid customerId, int year, int month)
        => $"summary:dashboard:{customerId:D}:{year}:{month}";

    public static string UnpaidSubscriptions(Guid customerId, int year, int month)
        => $"summary:unpaid:{customerId:D}:{year}:{month}";

    public static string SubscriptionsAll(Guid customerId)
        => $"subs:all:{customerId:D}";

    public static string SubscriptionsActiveAll()
        => "subs:active:all";

    public static string RefreshToken(string tokenHash)
        => $"auth:rt:{Normalize(tokenHash)}";

    public static string LogoutAfter(Guid customerId)
        => $"auth:logout_after:{customerId:D}";

    private static string Normalize(string raw)
    {
        return raw.Trim().ToLowerInvariant();
    }
}
