using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NosAyudamos
{
    public class Whatsapp
    {
        private readonly IMessaging _messaging;
        private readonly ILanguageUnderstanding _languageUnderstanding;

        public Whatsapp(IMessaging messaging, ILanguageUnderstanding languageUnderstanding)
        {
            _messaging = messaging;
            _languageUnderstanding = languageUnderstanding;
        }

        [FunctionName("whatsapp")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();

            var msg = Message.Create(body);

            var intents = await _languageUnderstanding.GetIntentsAsync(msg.Body);
            log.LogInformation(JsonSerializer.Serialize(intents, new JsonSerializerOptions { WriteIndented = true }));

            return new OkObjectResult("");
       }
    }
}
