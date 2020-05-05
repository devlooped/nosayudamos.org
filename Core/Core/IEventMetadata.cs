using System;

namespace NosAyudamos
{
    interface IEventMetadata
    {
        string EventId { get; }
        DateTime? EventTime { get; }
        string? Subject { get; }
        string? Topic { get; }
    }
}
