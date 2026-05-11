using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PayderPay.Infrastructure.Persistence;

public class PayderPayDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PayderPayDbContext>
{
    public PayderPayDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PayderPayDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5157;Database=payderpay_dev;Username=postgres;Password=postgres");

        return new PayderPayDbContext(optionsBuilder.Options);
    }
}
