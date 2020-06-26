namespace NosAyudamos
{
    /// <summary>
    /// After a <see cref="PaymentCodeReceived"/> is processed and validated 
    /// by the payment processor, a request is triggered with the full information 
    /// from the QR code so that validation can be performed, according to 
    /// the requested amount and other information.
    /// </summary>
    public class PaymentRequested
    {
        public PaymentRequested(double amount, string description, string merchant, string personId, string qrData)
            => (Amount, Description, Merchant, PersonId, QRData)
            = (amount, description, merchant, personId, qrData);

        /// <summary>
        /// Total amount requested for the payment.
        /// </summary>
        public double Amount { get; }
        /// <summary>
        /// Description from the first line item in the payment.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// Business that would receive the payment.
        /// </summary>
        public string Merchant { get; }

        /// <summary>
        /// Donee that requested the payment by sending the <see cref="QRData"/>.
        /// </summary>
        public string PersonId { get; }
        /// <summary>
        /// QR data that initiated the request.
        /// </summary>
        public string QRData { get; }
    }
}
