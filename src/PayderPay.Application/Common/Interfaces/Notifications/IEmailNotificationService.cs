using PayderPay.Domain.Enums;

namespace PayderPay.Application.Common.Interfaces.Notifications;

public interface IEmailNotificationService
{
    /// <summary>
    /// Sends a "your invoice due date is approaching" reminder. Customer name and subscriber
    /// number are masked in the email body; amount is shown openly.
    /// </summary>
    Task<EmailDispatchResult> SendUpcomingPaymentReminderAsync(
        UpcomingPaymentReminderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a payment receipt after a payment attempt (successful or failed). Customer name
    /// and subscriber number are masked; amount is shown openly.
    /// </summary>
    Task<EmailDispatchResult> SendPaymentReceiptAsync(
        PaymentReceiptRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record UpcomingPaymentReminderRequest(
    string ToEmail,
    string CustomerName,
    SubscriptionType SubscriptionType,
    string ProviderName,
    string SubscriberNumber,
    decimal Amount,
    DateOnly DueDate,
    int PeriodYear,
    int PeriodMonth);

public sealed record PaymentReceiptRequest(
    string ToEmail,
    string CustomerName,
    SubscriptionType SubscriptionType,
    string ProviderName,
    string SubscriberNumber,
    decimal Amount,
    DateTime PaidAtUtc,
    int PeriodYear,
    int PeriodMonth,
    PaymentStatus Status,
    string? ExternalTransactionId,
    string? FailureReason);

/// <summary>
/// Result of an email dispatch attempt. Callers use it to write a NotificationLog row
/// without coupling to the underlying SMTP implementation.
/// </summary>
public sealed record EmailDispatchResult(
    bool Sent,
    string Subject,
    string Body,
    DateTime? SentAtUtc,
    string? FailureReason);
