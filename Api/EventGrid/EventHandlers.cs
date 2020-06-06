using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System.Reflection;

namespace NosAyudamos.EventGrid
{
    class EventHandlers
    {
        readonly ISerializer serializer;
        readonly IServiceProvider services;

        public EventHandlers(ISerializer serializer, IServiceProvider services)
            => (this.serializer, this.services)
            = (serializer, services);

        [FunctionName("automation-paused")]
        public Task AutomationPausedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<AutomationPaused>(serializer));

        [FunctionName("automation-resumed")]
        public Task AutomationResumedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<AutomationResumed>(serializer));

        /// <summary>
        /// Initial handler of uncategorized incoming messages from event grid 
        /// callbacks into our azure function.
        /// <see cref="IEventHandler{TEvent}"/>.
        /// </summary>
        [FunctionName("message-received")]
        public Task MessageReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<MessageReceived>(serializer));

        [FunctionName("message-sent")]
        public Task MessageSentAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<MessageSent>(serializer));


        [FunctionName("language-trained")]
        public Task LanguageTrainedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<LanguageTrained>(serializer));


        [FunctionName("person-registered")]
        public Task PersonRegisteredAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PersonRegistered>(serializer));

        [FunctionName("registration-failed")]
        public Task RegistrationFailedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<RegistrationFailed>(serializer));


        [FunctionName("slack-message-sent")]
        public Task SlackMessageSentAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<SlackMessageSent>(serializer));

        [FunctionName("slack-event-received")]
        public Task SlackEventReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<SlackEventReceived>(serializer));


        [FunctionName("tax-status-accepted")]
        public Task TaxStatusAcceptedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<TaxStatusAccepted>(serializer));

        [FunctionName("tax-status-approved")]
        public Task TaxStatusApprovedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<TaxStatusApproved>(serializer));

        [FunctionName("tax-status-rejected")]
        public Task TaxStatusRejectedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<TaxStatusRejected>(serializer));

        [FunctionName("unknown-message-received")]
        public Task UnknownMessageReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<UnknownMessageReceived>(serializer));


        [FunctionName("donation-received")]
        public Task DonationReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<DonationReceived>(serializer));

        [FunctionName("subscription-received")]
        public Task SubscriptionReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<SubscriptionReceived>(serializer));


        [FunctionName("payment-code-received")]
        public Task PaymentCodeReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PaymentCodeReceived>(serializer));

        [FunctionName("payment-requested")]
        public Task PaymentRequestedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PaymentRequested>(serializer));

        [FunctionName("payment-approved")]
        public Task PaymentApproveddAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PaymentRequested>(serializer));


        async Task HandleAsync<TEvent>(TEvent e)
        {
            // TODO: we could also allow derived handlers invocation here....
            var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));
            foreach (var handler in handlers.OrderBy(h => h.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0))
            {
                await handler.HandleAsync(e);
            }
        }
    }
}
