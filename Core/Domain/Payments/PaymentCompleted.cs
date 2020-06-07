namespace NosAyudamos
{
    public class PaymentCompleted
    {
        public PaymentCompleted(double amount, string description, string merchant, string personId)
            => (Amount, Description, Merchant, PersonId)
            = (amount, description, merchant, personId);

        public double Amount { get; }
        public string Description { get; }
        public string Merchant { get; }

        public string PersonId { get; }
    }
}
