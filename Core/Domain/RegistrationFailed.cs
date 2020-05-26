using System;

namespace NosAyudamos
{
    public class RegistrationFailed
    {
        public RegistrationFailed(string phoneNumber, Uri[] images)
            => (PhoneNumber, Images)
            = (phoneNumber, images);

        public string PhoneNumber { get; }
        public Uri[] Images { get; }
    }
}
