using System;

namespace NosAyudamos
{
    public abstract class DomainEvent
    {
        protected DomainEvent() => EventId = Guid.NewGuid();

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Guid EventId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int Version { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
    }
}
