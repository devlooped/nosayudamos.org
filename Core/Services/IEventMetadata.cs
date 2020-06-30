using System;

namespace NosAyudamos
{
    /// <summary>
    /// Interface implemented by <see cref="DomainEvent"/> to provide 
    /// a mapping when pushing them to EventGrid.
    /// </summary>
    interface IEventMetadata
    {
        string EventId { get; }
        DateTime? EventTime { get; }
        string? Subject { get; }
        string? Topic { get; }
    }
}
