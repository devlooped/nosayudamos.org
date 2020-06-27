namespace NosAyudamos
{
    /// <summary>
    /// Represents a donation event received from the payment processor, 
    /// which can be used to retrieve the actual payload afterwards.
    /// </summary>
    public class DonationReceived
    {
        public DonationReceived(string paymentId) => PaymentId = paymentId;

        public string PaymentId { get; }
    }
}
