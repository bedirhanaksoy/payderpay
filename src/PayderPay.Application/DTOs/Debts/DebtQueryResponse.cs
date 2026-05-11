namespace PayderPay.Application.Dtos.Debts;

public class DebtQueryResponse
{
    public Guid SubscriptionId { get; set; }
    public string SubscriberNumber { get; set; } = string.Empty;
    public IReadOnlyList<DebtQueryHistoryItemResponse> Debts { get; set; } = Array.Empty<DebtQueryHistoryItemResponse>();
}
