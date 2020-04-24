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

namespace NosAyudamos
{
    class Whatsapp
    {
        readonly IEventStream events;
        readonly ILogger<Whatsapp> logger;
        readonly IStartupWorkflow workflow;

        public Whatsapp(IEventStream events, IStartupWorkflow workflow, ILogger<Whatsapp> logger)
        {
            this.events = events;
            this.workflow = workflow;
            this.logger = logger;
        }

        [FunctionName("whatsapp")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Contract.Assert(req != null);

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

                var msg = Message.Create(body);

                events.Push(msg);

                logger.LogInformation("Message: {@Message}", msg);

                await workflow.RunAsync(msg);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                throw;
            }
        }
    }
}
