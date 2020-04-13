using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Text.Json;
using System.Linq;

namespace NosAyudamos
{
    public static class whatsapp
    {
        [FunctionName("whatsapp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var request = await new StreamReader(req.Body).ReadToEndAsync();
            var query = HttpUtility.ParseQueryString(request);
            var dict = query.OfType<string>().ToDictionary(x => x, x => query.Get(x));

            var body = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            log.LogInformation(body);

            return new OkObjectResult("");     
       }
    }
}
