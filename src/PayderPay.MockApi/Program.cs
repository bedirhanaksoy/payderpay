var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    Application = "PayderPay.MockApi",
    Status = "Running",
    UtcNow = DateTime.UtcNow
}));

app.Run();
