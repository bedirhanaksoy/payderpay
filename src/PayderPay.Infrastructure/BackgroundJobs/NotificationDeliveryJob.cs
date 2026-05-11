using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Settings;

namespace PayderPay.Infrastructure.BackgroundJobs;

public class NotificationDeliveryJob
{
    private readonly IReminderService _reminderService;
    private readonly IOptions<ReminderJobSettings> _settings;
    private readonly ILogger<NotificationDeliveryJob> _logger;

    public NotificationDeliveryJob(
        IReminderService reminderService,
        IOptions<ReminderJobSettings> settings,
        ILogger<NotificationDeliveryJob> logger)
    {
        _reminderService = reminderService;
        _settings = settings;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        if (!_settings.Value.Enabled)
        {
            _logger.LogDebug("Notification delivery job is disabled.");
            return;
        }

        var referenceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _reminderService.RunNotificationDeliveryAsync(referenceDate, _settings.Value.MaxAttempts);
    }
}
