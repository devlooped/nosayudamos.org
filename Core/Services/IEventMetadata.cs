using System;

namespace NosAyudamos
{
    /// <summary>
    /// Interface implemented by <see cref="DomainEvent"/> to provide 
    /// a mapping when pushing events to the EventGrid.
    /// </summary>
    interface IEventMetadata
    {
        /// <summary>
        /// A globally unique identifier for the event.
        /// </summary>
        string EventId { get; }

        /// <summary>
        /// The time of the occurrence of the event.
        /// </summary>
        DateTime? EventTime { get; }

        /// <summary>
        /// The <see cref="IIdentifiable.Id"/> (or <see cref="DomainEvent.SourceId"/>) 
        /// for event-sourced events, or a generic one for system events.
        /// </summary>
        string? Subject { get; }

        /// <summary>
        /// Typically either <c>Domain</c> (used by <see cref="DomainEvent"/>) or 
        /// <c>System</c> for non-<see cref="DomainObject"/>-generated events.
        /// </summary>
        string? Topic { get; }
    }
}
