using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Serilog;

namespace NosAyudamos
{
    class EventGridLogger
    {
        [FunctionName("eventlogger")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "options", "post", Route = null)] HttpRequest req,
            ILogger logger)
        {
            if (req.Method.Equals("options", StringComparison.OrdinalIgnoreCase) &&
                req.Headers.TryGetValue("WebHook-Request-Callback", out var values) &&
                values.Count == 1 &&
                Uri.TryCreate(values[0], UriKind.Absolute, out var callback))
            {
                var code = callback.ParseQueryString().Get("id");
                return new OkObjectResult(new SubscriptionValidationResponse
                {
                    ValidationResponse = code
                });
            }

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            logger.Information(JsonSerializer.Serialize(JsonDocument.Parse(body).RootElement, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

            return new OkObjectResult("");
        }
    }
}
