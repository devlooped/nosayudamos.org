using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

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
        public Task<IActionResult> AutomationPausedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<AutomationPaused>(serializer));

        [FunctionName("automation-resumed")]
        public Task<IActionResult> AutomationResumedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<AutomationResumed>(serializer));

        /// <summary>
        /// Initial handler of uncategorized incoming messages from event grid 
        /// callbacks into our azure function.
        /// <see cref="IEventHandler{TEvent}"/>.
        /// </summary>
        [FunctionName("message-received")]
        public Task<IActionResult> MessageReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<MessageReceived>(serializer));

        [FunctionName("message-sent")]
        public Task<IActionResult> MessageSentAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<MessageSent>(serializer));


        [FunctionName("language-trained")]
        public Task<IActionResult> LanguageTrainedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<LanguageTrained>(serializer));


        [FunctionName("person-registered")]
        public Task<IActionResult> PersonRegisteredAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PersonRegistered>(serializer));

        [FunctionName("registration-failed")]
        public Task<IActionResult> RegistrationFailedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<RegistrationFailed>(serializer));


        [FunctionName("slack-message-sent")]
        public Task<IActionResult> SlackMessageSentAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<SlackMessageSent>(serializer));

        [FunctionName("slack-event-received")]
        public Task<IActionResult> SlackEventReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<SlackEventReceived>(serializer));


        [FunctionName("tax-status-accepted")]
        public Task<IActionResult> TaxStatusAcceptedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<TaxStatusAccepted>(serializer));

        [FunctionName("tax-status-approved")]
        public Task<IActionResult> TaxStatusApprovedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<TaxStatusApproved>(serializer));

        [FunctionName("tax-status-rejected")]
        public Task<IActionResult> TaxStatusRejectedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<TaxStatusRejected>(serializer));

        [FunctionName("unknown-message-received")]
        public Task<IActionResult> UnknownMessageReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<UnknownMessageReceived>(serializer));


        [FunctionName("donation-received")]
        public Task<IActionResult> DonationReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<DonationReceived>(serializer));

        [FunctionName("subscription-received")]
        public Task<IActionResult> SubscriptionReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<SubscriptionReceived>(serializer));


        [FunctionName("payment-code-received")]
        public Task<IActionResult> PaymentCodeReceivedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PaymentCodeReceived>(serializer));

        [FunctionName("payment-requested")]
        public Task<IActionResult> PaymentRequestedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PaymentRequested>(serializer));

        [FunctionName("payment-approved")]
        public Task<IActionResult> PaymentApprovedAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<PaymentRequested>(serializer));

        /// <summary>
        /// Executes the handlers and provides the result that event grid will use to determine success/failure of the 
        /// event handling invocation. See https://docs.microsoft.com/en-us/azure/event-grid/delivery-and-retry 
        /// for more on retry policies and status codes.
        /// </summary>
        async Task<IActionResult> HandleAsync<TEvent>(TEvent e)
        {
            // TODO: we could also allow derived handlers invocation here....
            var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));
            foreach (var handler in handlers.OrderBy(h => h.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0))
            {
                try
                {
                    await handler.HandleAsync(e);
                }
                catch (HttpStatusException he)
                {
                    return new StatusCodeResult((int)he.StatusCode);
                }
            }

            return new OkResult();
        }
    }
}
