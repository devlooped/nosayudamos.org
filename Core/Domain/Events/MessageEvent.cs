using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NosAyudamos
{
    public abstract class MessageEvent
    {
        protected MessageEvent(string phoneNumber) => PhoneNumber = phoneNumber;

        public string PhoneNumber { get; }

        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime When { get; set; } = DateTime.UtcNow;
    }
}
