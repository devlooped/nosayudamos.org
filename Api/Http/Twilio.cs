using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Linq;

namespace NosAyudamos.Http
{
    class Twilio
    {
        readonly IEventStreamAsync events;
        readonly ILogger<Twilio> logger;
        readonly IStartupWorkflow workflow;

        public Twilio(IEventStreamAsync events, IStartupWorkflow workflow, ILogger<Twilio> logger)
        {
            this.events = events;
            this.workflow = workflow;
            this.logger = logger;
        }

        [FunctionName("twilio")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
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
                var values = body.Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);

                if (values.TryGetValue("from", out var from))
                    from = from.Replace("whatsapp:", "", StringComparison.Ordinal).TrimStart('+').Trim();
                else
                    throw new ArgumentException("'from' is required");

                if (values.TryGetValue("to", out var to))
                    to = to.Replace("whatsapp:", "", StringComparison.Ordinal).TrimStart('+').Trim();
                else
                    throw new ArgumentException("'to' is required");

                if (!values.TryGetValue("body", out var message))
                    throw new ArgumentException("'body' is required");

                // Detect media attachment from payload.
                // NOTE: chat-api already sends the Url as the body, so no need to detect 
                // specifically for that case.
                if (values.TryGetValue("NumMedia", out var numMedia) &&
                    values.TryGetValue("MediaUrl" + numMedia, out var mediaUrl) &&
                    Uri.TryCreate(mediaUrl, UriKind.Absolute, out var mediaUri))
                {
                    message = mediaUrl;
                }

                await events.PushAsync(new NosAyudamos.MessageReceived(from, to, message));

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
