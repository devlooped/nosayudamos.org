using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace NosAyudamos.EventGrid
{
    class EventHandlers
    {
        readonly ISerializer serializer;
        readonly IServiceProvider services;

        public EventHandlers(ISerializer serializer, IServiceProvider services)
            => (this.serializer, this.services)
            = (serializer, services);


        /// <summary>
        /// Initial handler of uncategorized incoming messages from event grid 
        /// callbacks into our azure function.
        /// <see cref="IEventHandler{TEvent}"/>.
        /// </summary>
        [FunctionName("message-received")]
        public Task MessageReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<MessageReceived>(serializer));

        [FunctionName("unknown-message")]
        public Task UnknownMessageAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<UnknownMessageReceived>(serializer));

        [FunctionName("message-sent")]
        public Task MessageSentAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<MessageSent>(serializer));

        [FunctionName("person-registered")]
        public Task PersonRegisteredAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PersonRegistered>(serializer));

        [FunctionName("automation-paused")]
        public Task AutomationPausedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<AutomationPaused>(serializer));

        [FunctionName("automation-resumed")]
        public Task AutomationResumedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<AutomationResumed>(serializer));

        [FunctionName("language-trained")]
        public Task LanguageTrainedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<LanguageTrained>(serializer));

        [FunctionName("slack-message-sent")]
        public Task SlackMessageSentAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<SlackMessageSent>(serializer));

        [FunctionName("slack-event-received")]
        public Task SlackEventReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<SlackEventReceived>(serializer));

        async Task HandleAsync<TEvent>(TEvent e)
        {
            var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(e);
            }
        }
    }
}
