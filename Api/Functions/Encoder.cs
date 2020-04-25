using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Merq;
using System.Dynamic;

namespace NosAyudamos.Functions
{
    class Encoder
    {
        readonly IEventStream events;

        public Encoder(IEventStream events) => this.events = events;

        [FunctionName("encode")]
        public async Task<IActionResult> EncodeAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "base62/encode")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (!int.TryParse(body, out var number))
                return new BadRequestResult();

            dynamic e = new ExpandoObject();
            e.Body = body;
            e.Number = number;

            events.Push<ExpandoObject>(e);

            return new OkObjectResult(Base62.Encode(number));
        }

        [FunctionName("decode")]
        public async Task<IActionResult> DecodeAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "base62/decode")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(body))
                return new BadRequestResult();

            var number = Base62.Decode(body);

            dynamic e = new ExpandoObject();
            e.Body = body;
            e.Number = number;

            events.Push<ExpandoObject>(e);

            return new OkObjectResult(number);
        }
    }
}
