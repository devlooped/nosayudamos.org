using System;

namespace NosAyudamos
{
    /// <summary>
    /// Represents a QR code sent by the donee by taking a photo 
    /// from the screen offered by the shop.
    /// </summary>
    public class PaymentCodeReceived
    {
        public PaymentCodeReceived(Uri imageUri, string personId)
            => (ImageUri, PersonId)
            = (imageUri, personId);

        public Uri ImageUri { get; }
        public string PersonId { get; }
    }
}
