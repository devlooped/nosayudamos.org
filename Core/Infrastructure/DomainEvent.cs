using System;
using System.Globalization;

namespace NosAyudamos
{
    [NoExport]
    public abstract class DomainEvent : IEventMetadata
    {
        protected DomainEvent() => EventId = PreciseTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string EventId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public DateTime EventTime { get; set; } = DateTime.UtcNow;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string? SourceId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int Version { get; set; }

        DateTime? IEventMetadata.EventTime => EventTime;
        string? IEventMetadata.Subject => SourceId;
        string? IEventMetadata.Topic => "Domain";
    }
}
