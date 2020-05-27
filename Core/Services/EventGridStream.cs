using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Merq;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.VisualStudio.Threading;

namespace NosAyudamos
{
    class EventGridStream : EventStream, IEventStreamAsync, IDisposable
    {
        readonly ThreadLocal<bool?> asyncCall = new ThreadLocal<bool?>();
        readonly Lazy<Uri> gridUri;
        readonly Lazy<string> apiKey;
        readonly IServiceProvider services;
        readonly IEnvironment environment;
        readonly ISerializer serializer;
        readonly JoinableTaskFactory taskFactory;

        public EventGridStream(IServiceProvider services, IEnvironment environment, ISerializer serializer, JoinableTaskFactory taskFactory)
        {
            this.services = services;
            this.environment = environment;
            this.serializer = serializer;
            this.taskFactory = taskFactory;
            gridUri = new Lazy<Uri>(() => new Uri(environment.GetVariable("EventGridUrl")));
            apiKey = new Lazy<string>(() => environment.GetVariable("EventGridAccessKey"));
        }

        public async Task PushAsync<TEvent>(TEvent args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            try
            {
                // By setting this variable which is thread-local, the Push method below will not 
                // invoke the async handlers at all.
                asyncCall.Value = true;
                Push(args);
                await InvokeHandlersInDevelopmentAsync(args);
                await SendToGridAsync(args);
            }
            finally
            {
                asyncCall.Value = null;
            }
        }

        public override void Push<TEvent>(TEvent args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            base.Push(args);

            // TODO: bring the code from EventStream into this project, 
            // and remove the synchronous method entirely?
            if (!environment.IsDevelopment() && asyncCall.Value != true)
                throw new InvalidOperationException("In production, the PushAsync method should be used exclusively.");

            if (asyncCall.Value != true)
            {
                taskFactory.Run(async () =>
                {
                    await InvokeHandlersInDevelopmentAsync(args);
                    await SendToGridAsync(args);
                });
            }
        }

        async Task SendToGridAsync<TEvent>(TEvent args)
        {
            if (!environment.IsDevelopment() || environment.GetVariable("SendToGridInDevelopment", false))
            {
                var credentials = new TopicCredentials(apiKey.Value);
                var domain = gridUri.Value.Host;
                using var client = new EventGridClient(credentials);

                await client.PublishEventsAsync(domain, new List<EventGridEvent> { args!.ToEventGrid(serializer) });
            }
        }

        async Task InvokeHandlersInDevelopmentAsync<TEvent>(TEvent args)
        {
            // All of this is development time only, so it looks a bit hacky, 
            // but it's fine: it allows us to test end-to-end from acceptance 

            if (environment.IsDevelopment())
            {
                var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));

                if (handlers != null)
                {
                    foreach (var handler in handlers)
                    {
                        await handler.HandleAsync(args);
                    }
                }

                var type = args!.GetType().BaseType;
                while (type != null && type != typeof(object))
                {
                    var serviceType = typeof(IEnumerable<>).MakeGenericType(typeof(IEventHandler<>).MakeGenericType(type));
                    var baseHandlers = (IEnumerable<dynamic>)services.GetService(serviceType);

                    if (baseHandlers != null)
                    {
                        foreach (var handler in baseHandlers)
                        {
                            await handler.HandleAsync((dynamic)args);
                        }
                    }

                    type = type.BaseType;
                }
            }
        }

        public void Dispose() => asyncCall.Dispose();
    }
}
