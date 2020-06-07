using System;

namespace NosAyudamos
{
    public class PaymentCodeReceived
    {
        public PaymentCodeReceived(Uri imageUri, string personId)
            => (ImageUri, PersonId)
            = (imageUri, personId);

        public Uri ImageUri { get; }
        public string PersonId { get; }
    }
}
