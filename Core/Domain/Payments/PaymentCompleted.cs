namespace NosAyudamos
{
    /// <summary>
    /// Signals that a payment was completed for a donee.
    /// </summary>
    public class PaymentCompleted
    {
        public PaymentCompleted(double amount, string description, string merchant, string personId)
            => (Amount, Description, Merchant, PersonId)
            = (amount, description, merchant, personId);

        /// <summary>
        /// Total amount paid.
        /// </summary>
        public double Amount { get; }
        /// <summary>
        /// Description from the first line item in the payment.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// Business that will receive the payment.
        /// </summary>
        public string Merchant { get; }

        /// <summary>
        /// Donee that initiated the payment request.
        /// </summary>
        public string PersonId { get; }
    }
}
