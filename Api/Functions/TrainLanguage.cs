using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Functions
{
    class TrainLanguage
    {
        readonly ISerializer serializer;
        readonly ILanguageUnderstanding languageUnderstanding;
        readonly ILogger<TrainLanguage> logger;

        public TrainLanguage(ISerializer serializer, ILanguageUnderstanding languageUnderstanding, ILogger<TrainLanguage> logger) =>
            (this.serializer, this.languageUnderstanding, this.logger) = (serializer, languageUnderstanding, logger);

        [FunctionName("train_language")]
        public async Task<IActionResult> TrainAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "train_language")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var payload = await reader.ReadToEndAsync();
            dynamic body = serializer.Deserialize<JObject>(payload);

            await languageUnderstanding.AddUtteranceAsync((string)body.utterance, (string)body.intent);

            return new OkResult();
        }
    }
}
