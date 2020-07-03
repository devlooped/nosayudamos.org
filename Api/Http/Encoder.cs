using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace NosAyudamos
{
    class Encoder
    {
        [FunctionName("encode")]
        public IActionResult Encode([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "encoder/encode")] HttpRequest req)
        {
            if (!req.QueryString.HasValue ||
                !long.TryParse(req.QueryString.Value.TrimStart('?'), out var number))
                return new BadRequestObjectResult(Properties.Resources.Encoder_MissingEncodeQuery);

            return new OkObjectResult(Base62.Encode(number));
        }

        [FunctionName("decode")]
        public IActionResult Decode([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "encoder/decode")] HttpRequest req)
        {
            if (!req.QueryString.HasValue)
                return new BadRequestObjectResult(Properties.Resources.Encoder_MissingDecodeQuery);

            return new OkObjectResult(Base62.Decode(req.QueryString.Value.TrimStart('?')));
        }
    }
}
