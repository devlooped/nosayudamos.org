using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Merq;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;

namespace NosAyudamos
{
    [Shared]
    class EventGridStream : EventStream, IEventStreamAsync, IDisposable
    {
        readonly ThreadLocal<bool?> asyncCall = new ThreadLocal<bool?>();
        readonly Lazy<Uri> gridUri;
        readonly Lazy<string> apiKey;
        readonly IServiceProvider services;
        readonly IEnvironment environment;
        readonly ISerializer serializer;

        public EventGridStream(IServiceProvider services, IEnvironment environment, ISerializer serializer)
        {
            (this.services, this.environment, this.serializer) = (services, environment, serializer);
            gridUri = new Lazy<Uri>(() => new Uri(environment.GetVariable("EventGridUrl")));
            apiKey = new Lazy<string>(() => environment.GetVariable("EventGridAccessKey"));
        }

        public async Task PushAsync<TEvent>(TEvent args)
        {
            try
            {
                // By setting this variable which is thread-local, the Push method below will not 
                // invoke the async handlers at all.
                asyncCall.Value = true;
                Push(args);
                if (environment.IsDevelopment())
                {
                    var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));

                    foreach (var handler in handlers)
                    {
                        await handler.HandleAsync(args);
                    }
                }

                if (!environment.IsDevelopment() || environment.GetVariable("SendToGridInDevelopment", false))
                {
                    var credentials = new TopicCredentials(apiKey.Value);
                    var domain = gridUri.Value.Host;
                    using var client = new EventGridClient(credentials);

                    await client.PublishEventsAsync(domain, new List<EventGridEvent>
                    {
                        new EventGridEvent
                        {
                            Id = Guid.NewGuid().ToString(),
                            Topic = "NosAyudamos",
                            EventType = typeof(TEvent).FullName,
                            EventTime = DateTime.UtcNow,
                            Subject = typeof(TEvent).Namespace,
                            Data = serializer.Serialize(args),
                            DataVersion = "1.0",
                        }
                    });
                }
            }
            finally
            {
                asyncCall.Value = null;
            }
        }

        public override void Push<TEvent>(TEvent args)
        {
            base.Push(args);

            if (asyncCall.Value != true)
            {
                // This is duplicated but for a good reason: this should only be used in tests 
                // when the PushAsync cannot be used for some reason.
                if (environment.IsDevelopment())
                {
                    var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    Task.WaitAll(handlers.Select(handler => handler.HandleAsync(args)).ToArray());
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                }

                if (!environment.IsDevelopment() || environment.GetVariable("SendToGridInDevelopment", false))
                {
                    var credentials = new TopicCredentials(apiKey.Value);
                    var domain = gridUri.Value.Host;
                    using var client = new EventGridClient(credentials);

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    client.PublishEventsAsync(domain, new List<EventGridEvent>
                    {
                        new EventGridEvent
                        {
                            Id = Guid.NewGuid().ToString(),
                            Topic = "NosAyudamos",
                            EventType = typeof(TEvent).FullName,
                            EventTime = DateTime.UtcNow,
                            Subject = typeof(TEvent).Namespace,
                            Data = serializer.Serialize(args),
                            DataVersion = "1.0",
                        }
                    }).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                }
            }
        }

        public void Dispose() => asyncCall.Dispose();
    }
}
