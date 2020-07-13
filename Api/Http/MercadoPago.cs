using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace NosAyudamos.Http
{
    /// <summary>
    /// Processes the MP webhook event notification
    /// </summary>
    class MercadoPago
    {
        readonly IEventStreamAsync events;

        public MercadoPago(IEventStreamAsync events) => this.events = events;

        [FunctionName("mercadopago")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            // See https://www.mercadopago.com.ar/developers/es/guides/notifications/ipn
            var id = req.Query["id"];
            var topic = req.Query["topic"];

            // Quick path if the right values are already in the query string
            if (id != StringValues.Empty && topic != StringValues.Empty)
            {
                // By pushing to the event stream immediately and processing later, we get the 
                // required quick response expected by MP after the notification, and we get the 
                // resilient processing/storing like for all the other event processing we do.
                if (topic == "payment")
                    await events.PushAsync(new DonationReceived(id));
                else if (topic == "subscription")
                    await events.PushAsync(new SubscriptionReceived(id));
            }

            return new OkResult();
        }
    }
}
