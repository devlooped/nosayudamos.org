using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;

namespace NosAyudamos
{
    public class Encoder
    {
        [FunctionName("encode")]
        public async Task<IActionResult> EncodeAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "base62/encode")] HttpRequest req)
        {
            Contract.Assert(req != null);

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (!int.TryParse(body, out var number))
                return new BadRequestResult();

            return new OkObjectResult(Base62.Encode(number));
        }

        [FunctionName("decode")]
        public async Task<IActionResult> DecodeAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "base62/decode")] HttpRequest req)
        {
            Contract.Assert(req != null);

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(body))
                return new BadRequestResult();

            return new OkObjectResult(Base62.Decode(body));
        }
    }
}
