using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Merq;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using NosAyudamos.Events;

namespace NosAyudamos
{
    [Shared]
    class EventGridStream : EventStream
    {
        Lazy<Uri> gridUri;
        Lazy<string> apiKey;
        readonly IServiceProvider services;
        readonly IEnvironment environment;
        readonly ISerializer serializer;

        public EventGridStream(IServiceProvider services, IEnvironment environment, ISerializer serializer)
        {
            (this.services, this.environment, this.serializer) = (services, environment, serializer);
            gridUri = new Lazy<Uri>(() => new Uri(environment.GetVariable("EventGridUrl")));
            apiKey = new Lazy<string>(() => environment.GetVariable("EventGridAccessKey"));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:Use expression body for methods", Justification = "WIP")]
        public override void Push<TEvent>(TEvent @event)
        {
            base.Push(@event);

            if (environment.IsDevelopment())
            {
                var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                Task.WaitAll(handlers.Select(x => x.HandleAsync(@event)).ToArray());
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }

            if (!environment.IsDevelopment() || environment.GetVariable("SendToGridInDevelopment", false))
            {
                var credentials = new TopicCredentials(apiKey.Value);
                var domain = gridUri.Value.Host;
                using var client = new EventGridClient(credentials);

                client.PublishEventsAsync(domain, new List<EventGridEvent>
                {
                    new EventGridEvent
                    {
                        Id = Guid.NewGuid().ToString(),
                        Topic = "NosAyudamos",
                        EventType = typeof(TEvent).FullName,
                        EventTime = DateTime.UtcNow,
                        Subject = typeof(TEvent).Namespace,
                        Data = serializer.Serialize(@event),
                        DataVersion = "1.0",
                    }
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                }).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }
        }
    }
}
