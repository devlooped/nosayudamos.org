namespace NosAyudamos
{
    public class PaymentRequested
    {
        public PaymentRequested(double amount, string description, string merchant, string personId, string qrData)
            => (Amount, Description, Merchant, PersonId, QRData)
            = (amount, description, merchant, personId, qrData);

        public double Amount { get; }
        public string Description { get; }
        public string Merchant { get; }

        public string PersonId { get; }
        public string QRData { get; }
    }
}
