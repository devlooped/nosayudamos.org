using System;
using System.Collections.Generic;
using NosAyudamos.Properties;

namespace NosAyudamos
{
    abstract class DomainObject
    {
        Dictionary<Type, Action<DomainEvent>> handlers = new Dictionary<Type, Action<DomainEvent>>();
        List<DomainEvent>? events;
        List<DomainEvent>? history;

        // This is basically a sort of memento pattern to get the 
        // observable state changes for the object but represented 
        // as a list of events.
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public IEnumerable<DomainEvent> Events => events ??= new List<DomainEvent>();

        /// <summary>
        /// When the domain object is loaded from history, provides access to 
        /// all its past events.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public IEnumerable<DomainEvent> History => history ??= new List<DomainEvent>();

        /// <summary>
        /// Whether the domain object was created in a readonly manner, meaning 
        /// that events cannot be produced from it.
        /// </summary>
        public bool IsReadOnly { get; protected set; }

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

            // NOTE: we don't fail for generated events that don't have a handler 
            // because those just mean they are events important to the domain, but 
            // that don't cause state changes to the current domain object.
            if (handlers.TryGetValue(e.GetType(), out var handler))
                handler(e);

            events ??= new List<DomainEvent>();
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
