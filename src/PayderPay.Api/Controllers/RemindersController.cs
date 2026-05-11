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
    /// Test/operations endpoint. Manually runs invoice sync and notification delivery once.
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<ReminderRunResponse>> Run(
        CancellationToken cancellationToken)
    {
        var referenceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _reminderService.RunInvoiceSyncAndDeliveryAsync(
            referenceDate,
            _settings.Value.LeadDays,
            _settings.Value.MaxAttempts,
            cancellationToken);

        return Ok(result);
    }
}
