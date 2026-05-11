namespace PayderPay.Application.UnitTests;

public class InvoiceSyncJobTests
{
    [Fact(Skip = "Skeleton test. Implement repository/provider mocks for full verification.")]
    public Task RunInvoiceSyncAsync_HappyPath_ShouldUpsertInvoicesAndQueueNotifications()
    {
        // Arrange: active subscriptions + provider unpaid debts
        // Act: RunInvoiceSyncAsync(referenceDate)
        // Assert: created/updated/marked-paid counters + queued notifications
        return Task.CompletedTask;
    }

    [Fact(Skip = "Skeleton test. Implement provider timeout/5xx simulation.")]
    public Task RunInvoiceSyncAsync_WhenProviderUnavailable_ShouldShortCircuitAndContinueScheduling()
    {
        // Arrange: provider timeout/5xx
        // Act: RunInvoiceSyncAsync(referenceDate)
        // Assert: short-circuit flag + error counters + scheduling step executed
        return Task.CompletedTask;
    }

    [Fact(Skip = "Skeleton test. Implement unique idempotency collision simulation.")]
    public Task RunInvoiceSyncAsync_WhenDuplicateQueueKeyExists_ShouldNotInsertDuplicate()
    {
        // Arrange: existing queue item with same idempotency_key
        // Act: RunInvoiceSyncAsync(referenceDate)
        // Assert: duplicate ignored safely (no exception)
        return Task.CompletedTask;
    }
}
