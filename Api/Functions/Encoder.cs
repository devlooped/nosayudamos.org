using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace NosAyudamos
{
    class Encoder
    {
        public Encoder(ILogger logger) => logger.Information("Created new Encoder");

        [FunctionName("encode")]
        public async Task<IActionResult> EncodeAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "base62/encode")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (!int.TryParse(body, out var number))
                return new BadRequestResult();

            return new OkObjectResult(Base62.Encode(number));
        }

        [FunctionName("decode")]
        public async Task<IActionResult> DecodeAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "base62/decode")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(body))
                return new BadRequestResult();

            return new OkObjectResult(Base62.Decode(body));
        }
    }
}
