namespace NosAyudamos
{
    public class SubscriptionReceived
    {
        public SubscriptionReceived(string subscriptionId) => SubscriptionId = subscriptionId;

        public string SubscriptionId { get; }
    }
}
