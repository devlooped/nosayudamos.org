using System;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class DomainEventEntity : TableEntity
    {
        public string? EventId { get; set; }
        public string? EventType { get; set; }
        public string? Data { get; set; }
        public string? DataVersion { get; set; }
        public int Version { get; set; }
    }
}
