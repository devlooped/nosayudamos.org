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

        /// <devdoc>
        /// When surfacing the <see cref="EventId"/> for use outside the owning 
        /// <see cref="DomainObject"/>, we must ensure that the identifier is globally 
        /// unique. Since it's based on a (however precise) timing, we add the domain 
        /// object identifier as a suffix, which would guarantee uniqueness since it's 
        /// impossible to genenerate two identical identifiers in a single process 
        /// by using our <see cref="PreciseTime"/> (there's a unit test for that). And 
        /// it's highly unlikely that the same domain object will be processed simultaneously 
        /// and generate a new event at the exact same time in two processes or machines
        /// within a 10th of a microsecond of another.
        /// </devdoc>
        string IEventMetadata.EventId => EventId + "_" + SourceId;

        DateTime? IEventMetadata.EventTime => EventTime;

        string? IEventMetadata.Subject => SourceId;

        /// <summary>
        /// The other type of event is a System-generated one, outside of a <see cref="DomainObject"/>.
        /// </summary>
        string? IEventMetadata.Topic => "Domain";
    }
}
