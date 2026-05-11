using PayderPay.Api.Configuration;
using PayderPay.Api.Middleware;
using PayderPay.Infrastructure.DependencyInjection;

DotEnvLoader.LoadFromRepositoryRoot();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    Application = "PayderPay.Api",
    Status = "Running",
    UtcNow = DateTime.UtcNow
}));

app.Run();

public partial class Program;
