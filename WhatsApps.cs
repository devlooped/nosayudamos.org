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
        private readonly IMessaging messaging;
        private readonly ILanguageUnderstanding languageUnderstanding;

        private readonly ITextAnalysis textAnalysis;

        public Whatsapp(IMessaging messaging, ILanguageUnderstanding languageUnderstanding, ITextAnalysis textAnalysis)
        {
            this.messaging = messaging;
            this.languageUnderstanding = languageUnderstanding;
            this.textAnalysis = textAnalysis;
        }

        [FunctionName("whatsapp")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();

            var msg = Message.Create(body);

            var intents = await languageUnderstanding.GetIntentsAsync(msg.Body);
            log.LogInformation(JsonSerializer.Serialize(intents, new JsonSerializerOptions { WriteIndented = true }));
            var entities = await textAnalysis.GetentitiesAsync(msg.Body);
            log.LogInformation(JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true }));
            var keyPhrases = await textAnalysis.GetKeyPhrasesAsync(msg.Body);
            log.LogInformation(JsonSerializer.Serialize(keyPhrases, new JsonSerializerOptions { WriteIndented = true }));
            

            return new OkObjectResult("");
        }
    }
}
