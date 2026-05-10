using PayderPay.Application.Common;

namespace PayderPay.Application.UnitTests;

public class BillingDateHelperTests
{
    [Fact]
    public void CalculateDueDate_ShouldClampToMonthEnd_WhenDueDayExceedsMonthLength()
    {
        var dueDate = BillingDateHelper.CalculateDueDate(2026, 2, 31);

        Assert.Equal(new DateOnly(2026, 2, 28), dueDate);
    }

    [Fact]
    public void ResolvePeriod_ShouldReturnProvidedValues_WhenBothProvided()
    {
        var (year, month) = BillingDateHelper.ResolvePeriod(2027, 11);

        Assert.Equal(2027, year);
        Assert.Equal(11, month);
    }
}
