using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Contracts;

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
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Contract.Assert(req != null);

            try
            {
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync();
                var msg = Message.Create(body);

                if (Uri.TryCreate(msg.Body, UriKind.Absolute, out var uri))
                {
                    var person = await personRecognizer.RecognizeAsync(msg.Body);

                    return new OkObjectResult(person);
                }
                else
                {
                    var intents = await languageUnderstanding.GetIntentsAsync(msg.Body);
                    var entities = await textAnalysis.GetEntitiesAsync(msg.Body);
                    var keyPhrases = await textAnalysis.GetKeyPhrasesAsync(msg.Body);

                    return new OkObjectResult(new
                    {
                        intents,
                        entities,
                        keyPhrases,
                    });
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                throw;
            }
        }
    }
}
