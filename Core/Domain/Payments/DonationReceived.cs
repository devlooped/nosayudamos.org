namespace NosAyudamos
{
    public class DonationReceived
    {
        public DonationReceived(string paymentId) => PaymentId = paymentId;

        public string PaymentId { get; }
    }
}
