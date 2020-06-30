using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace NosAyudamos
{
    /// <summary>
    /// If the <see cref="DomainObject"/>-derived type implements this 
    /// interface, the <see cref="DomainEvent.EventId"/> can be made friendlier 
    /// and much shorter instead of being <see cref="Guid"/>, since the 
    /// uniqueness can be guaranteed by compunding it with the <see cref="PersonId"/>
    /// provided by the domain object.
    /// </summary>
    interface IIdentifiable
    {
        string Id { get; }
    }

    abstract class DomainObject
    {
        Dictionary<Type, Action<DomainEvent>> handlers = new Dictionary<Type, Action<DomainEvent>>();
        List<DomainEvent>? events;
        List<DomainEvent>? history;

        // This is basically a sort of memento pattern to get the 
        // observable state changes for the object but represented 
        // as a list of events.
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonIgnore]
        public IReadOnlyList<DomainEvent> Events => events ??= new List<DomainEvent>();

        /// <summary>
        /// When the domain object is loaded from history, provides access to 
        /// all its past events.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonIgnore]
        public IReadOnlyList<DomainEvent> History => history ??= new List<DomainEvent>();

        /// <summary>
        /// Whether the domain object was created in a readonly manner, meaning 
        /// that events cannot be produced from it.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonIgnore]
        public bool IsReadOnly { get; protected set; } = true;

        /// <summary>
        /// Version of the domain object when it was originally loaded. Enables 
        /// optimistic concurrency checks.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonIgnore]
        public int Version { get; internal set; }

        /// <summary>
        /// Accepts the pending events emitted by the domain object, and moves them to 
        /// the <see cref="History"/>, so that it looks as if it was freshly rehidrated 
        /// from it.
        /// </summary>
        public void AcceptEvents()
        {
            history ??= new List<DomainEvent>();
            history.AddRange(events ??= new List<DomainEvent>());
            events.Clear();
        }

        /// <summary>
        /// Registers a domain event handler.
        /// </summary>
        protected void Handles<T>(Action<T> handler) where T : DomainEvent => handlers.Add(typeof(T), e => handler((T)e));

        /// <summary>
        /// Raises and applies a new event of the specified type to the domain object.
        /// See <see cref="Raise{T}(T)"/>.
        /// </summary>
        protected void Raise<T>() where T : DomainEvent, new() => Raise(new T());

        /// <summary>
        /// Raises and applies an event to the domain object.
        /// The domain object should register handlers for relevant 
        /// domain events by calling <see cref="Handles{T}"/>.
        /// The handlers can perform the actual state changes to the 
        /// domain object.
        /// </summary>
        protected void Raise<T>(T e) where T : DomainEvent
        {
            if (IsReadOnly)
                throw new InvalidOperationException(Strings.DomainObject.ReadOnly);

            events ??= new List<DomainEvent>();

            var identifiable = this as IIdentifiable;
            var idSet = false;

            // If we can avoid it, let's try to create a friendlier and 
            // shorter identifier than a Guid (which is already created by default otherwise)
            // Note: we pad to 10 digits for consistency with StreamStone and because it seems 
            // large enough for a single root object entity.
            if (identifiable != null && 
                !string.IsNullOrEmpty(identifiable.Id))
            {
                e.EventId = identifiable.Id + '-' + (Version + events.Count + 1).ToString(CultureInfo.InvariantCulture).PadLeft(10, '0');
                idSet = true;
            }

            // NOTE: we don't fail for generated events that don't have a handler 
            // because those just mean they are events important to the domain, but 
            // that don't cause state changes to the current domain object.
            if (handlers.TryGetValue(e.GetType(), out var handler))
                handler(e);

            // It's typical during the initial ctor-called Raise to not have the Id property 
            // set yet, which would have been done in the handler instead, so set it right-after.
            // NOTE: this might cause handlers that consume the EventId to be out of sync, but 
            // that would be a quite unusal usage of the event sourced events anyway.
            if (!idSet && 
                identifiable != null)
            {
                e.EventId = identifiable.Id + '-' + (Version + events.Count + 1).ToString(CultureInfo.InvariantCulture).PadLeft(10, '0');
            }

            events.Add(e);
        }

        /// <summary>
        /// Loads the domain object by applying its historic events.
        /// </summary>
        /// <remarks>
        /// This method cannot be a constructor because derived classes need 
        /// to first set up their <see cref="Handles{T}(Action{T})"/> registrations 
        /// before we can apply the history.
        /// </remarks>
        protected void Load(IEnumerable<DomainEvent> history)
        {
            IsReadOnly = false;
            foreach (var e in history)
            {
                if (handlers.TryGetValue(e.GetType(), out var handler))
                    handler(e);

                this.history ??= new List<DomainEvent>();
                this.history.Add(e);
            }
        }
    }
}
