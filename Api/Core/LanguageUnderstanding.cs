using System;
using System.Collections.Generic;
using System.Composition;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace NosAyudamos
{
    [Shared]
    class LanguageUnderstanding : ILanguageUnderstanding
    {
        readonly IEnvironment enviroment;
        readonly IReadOnlyPolicyRegistry<string> registry;
        readonly ILogger<LanguageUnderstanding> logger;

        public LanguageUnderstanding(IEnvironment enviroment, IReadOnlyPolicyRegistry<string> registry, ILogger<LanguageUnderstanding> logger) =>
            (this.enviroment, this.registry, this.logger) = (enviroment, registry, logger);

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

            var policy = registry.Get<IAsyncPolicy>("LuisPolicy");

            var predictionResponse = await policy.ExecuteAsync(async () =>
                await luisClient.Prediction.GetSlotPredictionAsync(
                    Guid.Parse(enviroment.GetVariable("LuisAppId")),
                    slotName: enviroment.GetVariable("LuisAppSlot"),
                    predictionRequest,
                    verbose: true,
                    showAllIntents: false,
                    log: true).ConfigureAwait(false));

            return predictionResponse.Prediction.Intents.Keys;
        }

        private ILUISRuntimeClient CreateLuisClient()
        {
            var credentials = new ApiKeyServiceClientCredentials(
                enviroment.GetVariable("LuisSubscriptionKey"));

            return new LUISRuntimeClient(credentials, Array.Empty<DelegatingHandler>())
            {
                Endpoint = enviroment.GetVariable("LuisEndpoint")
            };
        }
    }

    interface ILanguageUnderstanding
    {
        Task<IEnumerable<string>> GetIntentsAsync(string? text);
    }

}