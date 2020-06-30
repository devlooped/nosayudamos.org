using System;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;
using Streamstone;

namespace NosAyudamos
{
    static class DomainExtensions
    {
        public static DomainEvent ToDomainEvent(this DomainEventEntity entity, ISerializer serializer)
        {
            if (entity.Data == null)
                throw new ArgumentException(Strings.DomainRepository.EmptyData);
            if (entity.EventType == null)
                throw new ArgumentException(Strings.DomainRepository.EmptyEventType);

            var entityType = Type.GetType(entity.EventType, true)!;
            if (!typeof(DomainEvent).IsAssignableFrom(entityType))
                throw new ArgumentException(Strings.DomainRepository.EmptyData);

            var e = (DomainEvent)serializer.Deserialize(entity.Data, entityType);
            e.SourceId = entity.PartitionKey;
            e.EventId = entity.EventId ?? Guid.NewGuid().ToString("n");
            e.Version = entity.Version;
            e.EventTime = entity.Timestamp.UtcDateTime;

            return e;
        }

        public static EventData ToEventData(this DomainEvent e, int version, params ITableEntity[] includes)
        {
            // Properties are turned into columns in the table, which can be 
            // convenient for quickly glancing at the data.
            var properties = new
            {
                // Visualizing the event id in the table as a column is useful for querying
                e.EventId,
                EventType = e.GetType().FullName,
                Data = new Serializer().Serialize(e),
                DataVersion = (e.GetType().Assembly.GetName().Version ?? new Version(1, 0)).ToString(2),
                Version = version,
            };

            return new EventData(
                // This turns off the SS-UID-[id] duplicate event detection rows, since 
                // we use GUIDs for events and therefore we'll never be duplicating
                EventId.None,
                EventProperties.From(properties),
                EventIncludes.From(includes.Select(x => Include.InsertOrReplace(x))));
        }
    }
}
