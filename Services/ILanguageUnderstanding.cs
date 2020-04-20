using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    interface ILanguageUnderstanding
    {
        Task<IEnumerable<string>> GetIntentsAsync(string? text);
    }

    class LanguageUnderstanding : ILanguageUnderstanding
    {
        readonly IEnvironment environment;
        readonly ILogger<LanguageUnderstanding> logger;

        public LanguageUnderstanding(IEnvironment environment, ILogger<LanguageUnderstanding> logger)
        {
            this.environment = environment;
            this.logger = logger;
        }

        public async Task<IEnumerable<string>> GetIntentsAsync(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            using var luisClient = CreateLuisClient();

            var requestOptions = new PredictionRequestOptions
            {
                PreferExternalEntities = true,
            };

            var predictionRequest = new PredictionRequest
            {
                Query = text,
                Options = requestOptions
            };

            var predictionResponse = await luisClient.Prediction.GetSlotPredictionAsync(
                Guid.Parse(environment.GetVariable("LuisAppId")),
                slotName: environment.GetVariable("LuisAppSlot"),
                predictionRequest,
                verbose: true,
                showAllIntents: false,
                log: true).ConfigureAwait(false);

            return predictionResponse.Prediction.Intents.Keys;
        }

        private ILUISRuntimeClient CreateLuisClient()
        {
            var credentials = new ApiKeyServiceClientCredentials(
                environment.GetVariable("LuisSubscriptionKey"));

            return new LUISRuntimeClient(credentials, Array.Empty<DelegatingHandler>())
            {
                Endpoint = environment.GetVariable("LuisEndpoint")
            };
        }
    }
}
