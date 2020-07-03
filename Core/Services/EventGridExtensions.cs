using System;
using System.Globalization;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;

namespace NosAyudamos
{
    static class EventGridExtensions
    {
        public static T GetData<T>(this EventGridEvent e, ISerializer serializer)
        {
            if (e.EventType != typeof(T).FullName)
                throw new NotSupportedException($"Expected {typeof(T).FullName} as event type but got {e.EventType}");

            return serializer.Deserialize<T>(e.Data.ToString()!);
        }

        public static object? GetData(this EventGridEvent e, ISerializer serializer)
        {
            var type = Type.GetType(e.EventType);
            // TODO: throw?
            if (type == null)
                return null;

            return serializer.Deserialize(e.Data.ToString()!, type);
        }

        public static EventGridEvent ToEventGrid(this object data, ISerializer serializer)
        {
            var metadata = data as IEventMetadata;
            var now = PreciseTime.UtcNow;

            return new EventGridEvent
            {
                Id = metadata?.EventId ?? now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("n"),
                EventType = data.GetType().FullName!,
                EventTime = metadata?.EventTime ?? now.DateTime,
                Data = serializer.Serialize(data),
                DataVersion = data.GetType().Assembly.GetName().Version?.ToString(2) ?? "1.0",

                Subject = metadata?.Subject ?? data.GetType().Namespace,
                // Unless the object itself provides a different default, 
                // like DomainEvent does, we send everything to the System 
                Topic = metadata?.Topic ?? "System",
            };
        }

        public static TableEntity ToEntity(this EventGridEvent e) => new EventGridEventEntity
        {
            PartitionKey = e.EventType,
            RowKey = e.Id,
            Data = e.Data.ToString(),
            DataVersion = e.DataVersion,

            Subject = e.Subject,
            // The actual topic contains a gigantic amount of useless jargon like:
            // /subscriptions/4498a56e-cfc2-4aec-927f-415b126251e0/resourceGroups/nosayudamos/providers/Microsoft.EventGrid/domains/nosayudamos/topics/NosAyudamos
            // The only useful bit of domain information is the actual topic at the end, which we could find a use for.
            Topic = e.Topic.Contains("/topics/", StringComparison.Ordinal)
                ? string.Join('/', e.Topic.Split('/').SkipWhile(x => !"topics".Equals(x, StringComparison.Ordinal)).Skip(1))
                : e.Topic,
        };

        public static TableEntity ToEntity(this DomainEvent data, ISerializer serializer)
        {
            var metadata = (IEventMetadata)data;

            return new EventGridEventEntity
            {
                PartitionKey = data.GetType().FullName!,
                RowKey = metadata.EventId,
                Data = serializer.Serialize(data),
                DataVersion = data.GetType().Assembly.GetName().Version?.ToString(2) ?? "1.0",
                
                Subject = metadata.Subject ?? data.GetType().Namespace,
                Topic =  metadata?.Topic ?? "System",
            };
        }

        class EventGridEventEntity : TableEntity
        {
            public string? Data { get; set; }
            public string? DataVersion { get; set; }

            public string? Subject { get; set; }
            public string? Topic { get; set; }
        }
    }
}
