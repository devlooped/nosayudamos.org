namespace NosAyudamos
{
    /// <summary>
    /// Payment approval for an outgoing payment with a QR, 
    /// initiated originally by the donee.
    /// </summary>
    public class PaymentApproved
    {
        public PaymentApproved(double amount, string description, string merchant, string personId, string qrData)
            => (Amount, Description, Merchant, PersonId, QRData)
            = (amount, description, merchant, personId, qrData);

        /// <summary>
        /// Total amount to pay.
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
        /// Donee that initiated the payment request that has been approved.
        /// </summary>
        public string PersonId { get; }
        /// <summary>
        /// Original QR data that initiated the payment.
        /// </summary>
        public string QRData { get; }
    }
}
