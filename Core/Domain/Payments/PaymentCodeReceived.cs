using System;

namespace NosAyudamos
{
    public class PaymentCodeReceived
    {
        public PaymentCodeReceived(string personId, Uri codeUri)
            => (PersonId, CodeUri)
            = (personId, codeUri);

        public string PersonId { get; }

        public Uri CodeUri { get; }
    }
}
