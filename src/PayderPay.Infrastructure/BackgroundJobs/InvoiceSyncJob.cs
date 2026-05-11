using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Settings;

namespace PayderPay.Infrastructure.BackgroundJobs;

public class InvoiceSyncJob
{
    private readonly IReminderService _reminderService;
    private readonly IOptions<ReminderJobSettings> _settings;
    private readonly ILogger<InvoiceSyncJob> _logger;

    public InvoiceSyncJob(
        IReminderService reminderService,
        IOptions<ReminderJobSettings> settings,
        ILogger<InvoiceSyncJob> logger)
    {
        _reminderService = reminderService;
        _settings = settings;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        if (!_settings.Value.Enabled)
        {
            _logger.LogDebug("Invoice sync job is disabled.");
            return;
        }

        var referenceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _reminderService.RunInvoiceSyncAsync(referenceDate, _settings.Value.LeadDays);
    }
}
