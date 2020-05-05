using System;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;

namespace NosAyudamos
{
    class EventGridEventEntity : TableEntity
    {
        public EventGridEventEntity() => PartitionKey = nameof(EventGridEvent);

        public string? Data { get; set; }
        public string? DataVersion { get; set; }
        public string? EventType { get; set; }
        public DateTime? EventTime { get; set; }
        public string? Subject { get; set; }
        public string? Topic { get; set; }
    }
}
