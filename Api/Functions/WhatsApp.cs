using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics.Contracts;
using System.Net;
using Serilog;

namespace NosAyudamos
{
    class Whatsapp
    {
        [FunctionName("whatsapp")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Resolve] IStartupWorkflow workflow, 
            [Resolve] ILogger logger)
        {
            Contract.Assert(req != null);

            using var reader = new StreamReader(req.Body);
            var raw = await reader.ReadToEndAsync();
            var body = WebUtility.UrlDecode(raw);

            if (req.IsTwilioRequest() && !req.IsTwilioSigned(raw))
            {
                logger.Warning("Received callback came from Twilio but is not properly signed.");
                return new BadRequestResult();
            }

            try
            {
                logger.Information("Raw: {Body}", raw);

                var msg = Message.Create(body);

                logger.Information("Message: {@Message}", msg);

                await workflow.RunAsync(msg);

                return new OkResult();
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                throw;
            }
        }
    }
}
