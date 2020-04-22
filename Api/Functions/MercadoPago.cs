using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Serilog;

namespace NosAyudamos
{
    class MercadoPago
    {
        readonly ILogger logger;

        public MercadoPago(ILogger logger) => this.logger = logger;

        [FunctionName("mercadopago")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            logger.Information(JsonSerializer.Serialize(JsonDocument.Parse(body).RootElement, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

            return new OkObjectResult("");
        }
    }
}
