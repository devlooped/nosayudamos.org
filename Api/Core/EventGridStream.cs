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

        public async Task PushAsync<TEvent>(TEvent args, EventMetadata? metadata = null)
        {
            try
            {
                // By setting this variable which is thread-local, the Push method below will not 
                // invoke the async handlers at all.
                asyncCall.Value = true;
                Push(args);
                await InvokeHandlersAsync(args);
                await SendToGridAsync(args, metadata);
            }
            finally
            {
                asyncCall.Value = null;
            }
        }

        public override void Push<TEvent>(TEvent args)
        {
            base.Push(args);

            // TODO: bring the code from EventStream into this project, 
            // and remove the synchronous method entirely?
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("In production, the PushAsync method should be used exclusively.");

            if (asyncCall.Value != true)
            {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                InvokeHandlersAsync(args).Wait();
                SendToGridAsync(args, null).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }
        }

        async Task SendToGridAsync<TEvent>(TEvent args, EventMetadata? metadata)
        {
            if (!environment.IsDevelopment() || environment.GetVariable("SendToGridInDevelopment", false))
            {
                var credentials = new TopicCredentials(apiKey.Value);
                var domain = gridUri.Value.Host;
                using var client = new EventGridClient(credentials);

                await client.PublishEventsAsync(domain, new List<EventGridEvent>
                    {
                        new EventGridEvent
                        {
                            Id = metadata?.EventId ?? Guid.NewGuid().ToString(),
                            Subject = metadata?.Subject ?? typeof(TEvent).Namespace,
                            Topic = metadata?.Topic ?? "NosAyudamos",

                            EventType = typeof(TEvent).FullName,
                            EventTime = DateTime.UtcNow,
                            Data = serializer.Serialize(args),
                            DataVersion = "1.0",
                        }
                    });
            }
        }

        async Task InvokeHandlersAsync<TEvent>(TEvent args)
        {
            if (environment.IsDevelopment())
            {
                var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));

                foreach (var handler in handlers)
                {
                    await handler.HandleAsync(args);
                }
            }
        }

        public void Dispose() => asyncCall.Dispose();
    }
}
