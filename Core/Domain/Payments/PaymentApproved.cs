using System;

namespace NosAyudamos
{
    public class PaymentApproved
    {
        public PaymentApproved(string personId, Uri codeUri, double amount, string description)
            => (PersonId, CodeUri, Amount, Description)
            = (personId, codeUri, amount, description);

        public string PersonId { get; set; }
        public Uri CodeUri { get; }

        public double Amount { get; set; }
        public string Description { get; set; }
    }
}
