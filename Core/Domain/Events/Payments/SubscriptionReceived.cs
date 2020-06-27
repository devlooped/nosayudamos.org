namespace NosAyudamos
{
    /// <summary>
    /// Represents a subscription received from the payment processor, 
    /// which can be used to retrieve the actual payload afterwards.
    /// </summary>
    public class SubscriptionReceived
    {
        public SubscriptionReceived(string subscriptionId) => SubscriptionId = subscriptionId;

        public string SubscriptionId { get; }
    }
}
