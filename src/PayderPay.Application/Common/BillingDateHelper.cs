namespace PayderPay.Application.Common;

public static class BillingDateHelper
{
    public static DateOnly CalculateDueDate(int year, int month, int dueDayOfMonth)
    {
        var clampedDay = Math.Min(dueDayOfMonth, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, clampedDay);
    }

    public static (int Year, int Month) ResolvePeriod(int? periodYear, int? periodMonth)
    {
        var now = DateTime.UtcNow;
        return (periodYear ?? now.Year, periodMonth ?? now.Month);
    }
}
