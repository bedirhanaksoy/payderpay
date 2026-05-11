using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayderPay.Application.Common.Interfaces.Notifications;
using PayderPay.Application.Common.Settings;
using PayderPay.Domain.Enums;

namespace PayderPay.Infrastructure.Notifications;

public class SmtpEmailNotificationService : IEmailNotificationService
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo TurkishTimeZone = ResolveTurkishTimeZone();

    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailNotificationService> _logger;

    public SmtpEmailNotificationService(
        IOptions<SmtpSettings> settings,
        ILogger<SmtpEmailNotificationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<EmailDispatchResult> SendUpcomingPaymentReminderAsync(
        UpcomingPaymentReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var subject = $"PayderPay ödeme hatırlatma — {SubscriptionLabel(request.SubscriptionType)}";
        var customerName = HtmlEncoder.Default.Encode(request.CustomerName);
        var subscriberNumber = HtmlEncoder.Default.Encode(request.SubscriberNumber);
        var providerName = HtmlEncoder.Default.Encode(request.ProviderName);
        var amount = HtmlEncoder.Default.Encode(FormatAmount(request.Amount));
        var dueDate = HtmlEncoder.Default.Encode(request.DueDate.ToString("dd.MM.yyyy", TurkishCulture));
        var period = HtmlEncoder.Default.Encode($"{request.PeriodYear}/{request.PeriodMonth:D2}");

        var body =
            "<p>Sayın <strong>" + customerName + "</strong>,</p>" +
            "<p>Yaklaşan bir aboneliğiniz için ödeme tarihi yaklaşıyor:</p>" +
            "<table style=\"border-collapse:collapse;\">" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Hizmet</td><td style=\"padding:4px 12px;\">" + providerName + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Abonelik No</td><td style=\"padding:4px 12px;\">" + subscriberNumber + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Dönem</td><td style=\"padding:4px 12px;\">" + period + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Son Ödeme Tarihi</td><td style=\"padding:4px 12px;\">" + dueDate + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Tutar</td><td style=\"padding:4px 12px;font-weight:700;\">" + amount + "</td></tr>" +
            "</table>" +
            "<p>Ödeme tarihinden önce PayderPay üzerinden ödeyebilirsiniz.</p>" +
            "<p style=\"color:#999;font-size:12px;\">Bu bildirim PayderPay tarafından otomatik gönderilmiştir.</p>";

        return SendAsync(request.ToEmail, subject, body, cancellationToken);
    }

    public Task<EmailDispatchResult> SendPaymentReceiptAsync(
        PaymentReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        var statusLabel = request.Status == PaymentStatus.Successful ? "başarıyla tamamlandı" : "başarısız oldu";
        var subject = $"PayderPay ödeme makbuzu — {SubscriptionLabel(request.SubscriptionType)}";
        var customerName = HtmlEncoder.Default.Encode(request.CustomerName);
        var subscriberNumber = HtmlEncoder.Default.Encode(request.SubscriberNumber);
        var providerName = HtmlEncoder.Default.Encode(request.ProviderName);
        var amount = HtmlEncoder.Default.Encode(FormatAmount(request.Amount));
        var paidAt = HtmlEncoder.Default.Encode(FormatAsTurkishTime(request.PaidAtUtc));
        var period = HtmlEncoder.Default.Encode($"{request.PeriodYear}/{request.PeriodMonth:D2}");
        var transactionId = HtmlEncoder.Default.Encode(request.ExternalTransactionId ?? "-");
        var failureLine = request.Status == PaymentStatus.Failed && !string.IsNullOrWhiteSpace(request.FailureReason)
            ? "<p style=\"color:#b91c1c;\">Hata: " + HtmlEncoder.Default.Encode(request.FailureReason) + "</p>"
            : string.Empty;

        var body =
            "<p>Sayın <strong>" + customerName + "</strong>,</p>" +
            "<p>Aboneliğiniz için ödemeniz <strong>" + statusLabel + "</strong>.</p>" +
            "<table style=\"border-collapse:collapse;\">" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Hizmet</td><td style=\"padding:4px 12px;\">" + providerName + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Abonelik No</td><td style=\"padding:4px 12px;\">" + subscriberNumber + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Dönem</td><td style=\"padding:4px 12px;\">" + period + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">İşlem Tarihi</td><td style=\"padding:4px 12px;\">" + paidAt + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">İşlem No</td><td style=\"padding:4px 12px;\">" + transactionId + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Tutar</td><td style=\"padding:4px 12px;font-weight:700;\">" + amount + "</td></tr>" +
            "</table>" +
            failureLine +
            "<p style=\"color:#999;font-size:12px;\">Bu makbuz PayderPay tarafından otomatik gönderilmiştir.</p>";

        return SendAsync(request.ToEmail, subject, body, cancellationToken);
    }

    public Task<EmailDispatchResult> SendInvoiceDueReminderAsync(
        InvoiceDueReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[{request.ProviderName}] Son ödeme tarihiniz yaklaşıyor";
        var customerName = HtmlEncoder.Default.Encode(request.CustomerName);
        var providerName = HtmlEncoder.Default.Encode(request.ProviderName);
        var subscriberNumber = HtmlEncoder.Default.Encode(request.SubscriberNumber);
        var amount = HtmlEncoder.Default.Encode($"{request.Amount:N2} {request.Currency}");
        var dueDate = HtmlEncoder.Default.Encode(request.DueDate.ToString("dd MMMM yyyy", TurkishCulture));
        var daysLeft = request.DaysLeft < 0 ? 0 : request.DaysLeft;

        var body =
            "<p>Sayın <strong>" + customerName + "</strong>,</p>" +
            "<p><strong>" + providerName + "</strong> aboneliğiniz için son ödeme tarihi yaklaşıyor.</p>" +
            "<table style=\"border-collapse:collapse;\">" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Abonelik</td><td style=\"padding:4px 12px;\">" + providerName + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Abone No</td><td style=\"padding:4px 12px;\">" + subscriberNumber + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Tutar</td><td style=\"padding:4px 12px;font-weight:700;\">" + amount + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Son Ödeme Tarihi</td><td style=\"padding:4px 12px;\">" + dueDate + "</td></tr>" +
            "<tr><td style=\"padding:4px 12px;color:#666;\">Kalan Gün</td><td style=\"padding:4px 12px;\">" + daysLeft + "</td></tr>" +
            "</table>" +
            "<p>Ödeme yaptıysanız bu mesajı dikkate almayınız.</p>" +
            "<p style=\"color:#999;font-size:12px;\">Bu bildirim PayderPay tarafından otomatik gönderilmiştir.</p>";

        return SendAsync(request.ToEmail, subject, body, cancellationToken);
    }

    private async Task<EmailDispatchResult> SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || _settings.Port <= 0 || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            const string reason = "SMTP settings are not configured.";
            _logger.LogWarning("Skipping email to {Recipient} ({Subject}) — {Reason}", recipient, subject, reason);
            return new EmailDispatchResult(false, subject, htmlBody, null, reason);
        }

        var fromName = string.IsNullOrWhiteSpace(_settings.FromName) ? _settings.FromEmail : _settings.FromName;

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        try
        {
            using var _ = cancellationToken.Register(() => client.SendAsyncCancel());
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Recipient} — {Subject}", recipient, subject);
            return new EmailDispatchResult(true, subject, htmlBody, DateTime.UtcNow, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} — {Subject}", recipient, subject);
            return new EmailDispatchResult(false, subject, htmlBody, null, ex.Message);
        }
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("N2", TurkishCulture) + " ₺";
    }

    private static string FormatAsTurkishTime(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, TurkishTimeZone);
        return local.ToString("dd.MM.yyyy HH:mm", TurkishCulture);
    }

    private static string SubscriptionLabel(SubscriptionType type) => type switch
    {
        SubscriptionType.Electricity => "Elektrik",
        SubscriptionType.Water => "Su",
        SubscriptionType.Internet => "İnternet",
        SubscriptionType.GSM => "GSM",
        SubscriptionType.NaturalGas => "Doğalgaz",
        _ => "Diğer"
    };

    private static TimeZoneInfo ResolveTurkishTimeZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
