using System;

namespace NosAyudamos
{
    public abstract class MessageEvent
    {
        protected MessageEvent(string phoneNumber) => PhoneNumber = phoneNumber;

        public string PhoneNumber { get; }

        public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
    }
}
