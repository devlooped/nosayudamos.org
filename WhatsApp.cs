using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Contracts;
using System.Xml.Linq;
using Twilio.TwiML;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;

namespace NosAyudamos
{
    public class Whatsapp
    {
        readonly IMessaging messaging;
        readonly ILanguageUnderstanding languageUnderstanding;
        readonly ITextAnalysis textAnalysis;
        readonly IPersonRecognizer personRecognizer;
        readonly ILogger<Whatsapp> logger;

        public Whatsapp(IMessaging messaging, ILanguageUnderstanding languageUnderstanding, ITextAnalysis textAnalysis, IPersonRecognizer personRecognizer, ILogger<Whatsapp> logger)
        {
            this.messaging = messaging;
            this.languageUnderstanding = languageUnderstanding;
            this.textAnalysis = textAnalysis;
            this.personRecognizer = personRecognizer;
            this.logger = logger;
        }

        [FunctionName("whatsapp")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Contract.Assert(req != null);

            if (req.IsTwilioRequest() && !req.IsTwilioSigned())
            {
                logger.LogWarning("Received callback came from Twilio but is not properly signed.");
                return new BadRequestResult();
            }

            try
            {
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync();

                logger.LogInformation("Request: {Body}", body);

                var msg = Message.Create(body);

                logger.LogInformation("Message: {@Message}", msg);

                if (Uri.TryCreate(msg.Body, UriKind.Absolute, out var uri))
                {
                    var person = await personRecognizer.RecognizeAsync(uri);

                    logger.LogInformation("Person: {@Person}", person);

                    return new OkObjectResult(person);
                }
                else
                {
                    var intents = await languageUnderstanding.GetIntentsAsync(msg.Body);
                    var entities = await textAnalysis.GetEntitiesAsync(msg.Body);
                    var keyPhrases = await textAnalysis.GetKeyPhrasesAsync(msg.Body);

                    var result = new
                    {
                        intents,
                        entities,
                        keyPhrases,
                    };

                    logger.LogInformation("Cognitive: {@Result}", result);

                    await messaging.SendTextAsync(msg.To!, "Gracias!", msg.From!);

                    if (req.IsTwilioRequest())
                        return new TwiMLResult(new MessagingResponse());

                    return new OkObjectResult(result);
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
