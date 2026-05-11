using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Settings;
using PayderPay.Application.Dtos.Reminders;
using Microsoft.Extensions.Options;

namespace PayderPay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reminders")]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;
    private readonly IOptions<ReminderJobSettings> _settings;

    public RemindersController(IReminderService reminderService, IOptions<ReminderJobSettings> settings)
    {
        _reminderService = reminderService;
        _settings = settings;
    }

    /// <summary>
    /// Manually scans active subscriptions whose due date falls within <c>leadDays</c> from
    /// <c>referenceDate</c> (defaults to today and the configured lead days), queries the
    /// 3rd-party debt provider, and sends a reminder email per subscription. Idempotent:
    /// reminders already sent for the current period are skipped.
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<IReadOnlyList<SendReminderResultResponse>>> Run(
        [FromQuery] DateOnly? referenceDate,
        [FromQuery] int? leadDays,
        CancellationToken cancellationToken)
    {
        var date = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var lead = leadDays ?? _settings.Value.LeadDays;

        var results = await _reminderService.SendDuePaymentRemindersAsync(date, lead, cancellationToken);
        return Ok(results);
    }
}
