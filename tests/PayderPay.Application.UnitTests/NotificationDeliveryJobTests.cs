namespace PayderPay.Application.UnitTests;

public class NotificationDeliveryJobTests
{
    [Fact(Skip = "Skeleton test. Implement queue + mail success path.")]
    public Task RunNotificationDeliveryAsync_HappyPath_ShouldMarkSent()
    {
        // Arrange: pending queue item, unpaid invoice, mail success
        // Act: RunNotificationDeliveryAsync(referenceDate)
        // Assert: status sent + sent_at set
        return Task.CompletedTask;
    }

    [Fact(Skip = "Skeleton test. Implement mail failure retry path.")]
    public Task RunNotificationDeliveryAsync_WhenMailFails_ShouldRetryAndEventuallyFail()
    {
        // Arrange: pending queue item, mail send failure
        // Act: RunNotificationDeliveryAsync(referenceDate)
        // Assert: attempts increment + pending/failed transition
        return Task.CompletedTask;
    }

    [Fact(Skip = "Skeleton test. Implement already-paid guard path.")]
    public Task RunNotificationDeliveryAsync_WhenInvoiceAlreadyClosed_ShouldSkipWithReason()
    {
        // Arrange: queue item with invoice no longer unpaid
        // Act: RunNotificationDeliveryAsync(referenceDate)
        // Assert: no send attempt + queue marked as closed/failed policy
        return Task.CompletedTask;
    }
}
