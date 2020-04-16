using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System;

namespace NosAyudamos
{
    public class Whatsapp
    {
        private readonly IMessaging messaging;
        private readonly ILanguageUnderstanding languageUnderstanding;

        private readonly ITextAnalysis textAnalysis;
        private readonly IPersonRecognizer personRecognizer;

        public Whatsapp(IMessaging messaging, ILanguageUnderstanding languageUnderstanding, ITextAnalysis textAnalysis, IPersonRecognizer personRecognizer)
        {
            this.messaging = messaging;
            this.languageUnderstanding = languageUnderstanding;
            this.textAnalysis = textAnalysis;
            this.personRecognizer = personRecognizer;
        }

        [FunctionName("whatsapp")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var msg = Message.Create(body);

                // log.LogInformation(JsonSerializer.Serialize(msg, new JsonSerializerOptions { WriteIndented = true }));
                // var intents = await languageUnderstanding.GetIntentsAsync(msg.Body);
                // log.LogInformation(JsonSerializer.Serialize(intents, new JsonSerializerOptions { WriteIndented = true }));
                // var entities = await textAnalysis.GetentitiesAsync(msg.Body);
                // log.LogInformation(JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true }));
                // var keyPhrases = await textAnalysis.GetKeyPhrasesAsync(msg.Body);
                // log.LogInformation(JsonSerializer.Serialize(keyPhrases, new JsonSerializerOptions { WriteIndented = true }));
                
                // return new OkObjectResult(new
                // {
                //     intents = intents,
                //     entities = entities,
                //     keyPhrases = keyPhrases,
                // });

                var person = await personRecognizer.Recognize(msg.Body);

                return new OkObjectResult(person);

            }
            catch (Exception ex)
            {
                log.LogError(ex, "An Exception ocurred.");

                throw;
            }
        }
    }
}
