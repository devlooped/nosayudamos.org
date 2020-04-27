using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Contracts;
using System.Net;
using Merq;
using NosAyudamos.Events;
using System.Linq;

namespace NosAyudamos.Functions
{
    class Twilio
    {
        readonly IEventStream events;
        readonly ILogger<Twilio> logger;
        readonly IStartupWorkflow workflow;

        public Twilio(IEventStream events, IStartupWorkflow workflow, ILogger<Twilio> logger)
        {
            this.events = events;
            this.workflow = workflow;
            this.logger = logger;
        }

        [FunctionName("twilio")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var raw = await reader.ReadToEndAsync();
            var body = WebUtility.UrlDecode(raw);

            if (req.IsTwilioRequest() && !req.IsTwilioSigned(raw))
            {
                logger.LogWarning("Received callback came from Twilio but is not properly signed.");
                return new BadRequestResult();
            }

            try
            {
                logger.LogInformation("Raw: {Body}", raw);

                var values = body.Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);

                var message = Message.Create(values);

                // Detect media attachment from payload.
                // NOTE: chat-api already sends the Url as the body, so no need to detect 
                // specifically for that case.
                if (values.TryGetValue("NumMedia", out var numMedia) &&
                    values.TryGetValue("MediaUrl" + numMedia, out var mediaUrl) &&
                    Uri.TryCreate(mediaUrl, UriKind.Absolute, out var mediaUri))
                {
                    message.Body = mediaUrl;
                }

                events.Push(new MessageReceived(message.From, message.To, message.Body));

                logger.LogInformation("Message: {@Message}", message);

                await workflow.RunAsync(message);

                return new OkResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                throw;
            }
        }
    }
}
