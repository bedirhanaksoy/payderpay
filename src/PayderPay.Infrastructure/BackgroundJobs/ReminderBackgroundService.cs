using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Settings;

namespace PayderPay.Infrastructure.BackgroundJobs;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ReminderJobSettings> _settings;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<ReminderJobSettings> settings,
        ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Value.Enabled)
        {
            _logger.LogInformation("Reminder background service is disabled.");
            return;
        }

        var interval = Math.Max(_settings.Value.IntervalMinutes, 1);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(interval));

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunReminderCycleAsync(stoppingToken);

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task RunReminderCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var reminderService = scope.ServiceProvider.GetService<IReminderService>();

            if (reminderService is null)
            {
                _logger.LogDebug("IReminderService is not registered yet. Skipping reminder cycle.");
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var results = await reminderService.SendDuePaymentRemindersAsync(today, _settings.Value.LeadDays, cancellationToken);

            _logger.LogInformation("Reminder cycle completed. Processed reminder results count: {Count}", results.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful cancellation.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reminder cycle failed.");
        }
    }
}
