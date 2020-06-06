using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

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
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            /*
            {
              "resource": "https://api.mercadolibre.com/collections/notifications/ID",
              "topic": "payment"
            }
            */

            var payload = JObject.Parse(body);

            var resource = payload.Property("resource", StringComparison.Ordinal)?.Value?.ToString();
            var topic = payload.Property("topic", StringComparison.Ordinal)?.Value?.ToString();

            if (resource == null || topic == null)
                return new BadRequestObjectResult("resource and topic are required");

            if (!Uri.TryCreate(resource, UriKind.Absolute, out var uri))
                return new BadRequestObjectResult("resource must be a valid URI");


            var id = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).Split('/').LastOrDefault();

            if (id == null)
                return new BadRequestObjectResult("invalid resource URI");

            // By pushing to the event stream immediately and processing later, we get the 
            // required quick response expected by MP after the notification, and we get the 
            // resilient processing/storing like for all the other event processing we do.
            if (topic == "payment")
                await events.PushAsync(new DonationReceived(id));
            else if (topic == "subscription")
                await events.PushAsync(new SubscriptionReceived(id));

            return new OkObjectResult("");
        }
    }
}
