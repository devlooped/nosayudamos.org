using System;
using System.Linq;
using System.Collections.Generic;
using System.Composition;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring;
using Authoring = Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring;
using Runtime = Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;

namespace NosAyudamos
{
    [Shared]
    class LanguageUnderstanding : ILanguageUnderstanding
    {
        static readonly Prediction emptyPrediction = new Prediction
        {
            Entities = new Dictionary<string, object>(),
            Intents = new Dictionary<string, Intent>(),
            Sentiment = new Sentiment()
        };

        readonly IEnvironment enviroment;
        readonly IReadOnlyPolicyRegistry<string> registry;
        readonly ILogger<LanguageUnderstanding> logger;

        public LanguageUnderstanding(IEnvironment enviroment, IReadOnlyPolicyRegistry<string> registry, ILogger<LanguageUnderstanding> logger) =>
            (this.enviroment, this.registry, this.logger) = (enviroment, registry, logger);

        public async Task AddUtteranceAsync(string? utterance, string? intent)
        {
            if (utterance == null || intent == null)
            {
                return;
            }

            using var luisClient = CreateLuisAuthoringClient();

            var app = await luisClient.Apps.GetAsync(Guid.Parse(enviroment.GetVariable("LuisAppId")));

            if (app != null && app.Id != null)
            {
                var intents = await luisClient.Model.ListIntentsAsync((Guid)app.Id!, app.ActiveVersion);

                var intentClassifier = intents.FirstOrDefault(i => i.Name.Equals(intent, StringComparison.OrdinalIgnoreCase));

                if (intentClassifier != null)
                {
                    await luisClient.Examples.AddAsync(
                        (Guid)app.Id!,
                        app.ActiveVersion,
                        new ExampleLabelObject { IntentName = intent, Text = utterance });

                    await luisClient.Train.TrainVersionAsync((Guid)app.Id!, app.ActiveVersion);

                    await luisClient.Apps.PublishAsync(
                        (Guid)app.Id!,
                        new ApplicationPublishObject
                        {
                            VersionId = app.ActiveVersion,
                            IsStaging = enviroment.GetVariable<string>("LuisAppSlot", "staging").Equals("staging", StringComparison.OrdinalIgnoreCase)
                        });
                }
            }
        }

        public async Task<Prediction> PredictAsync(string text)
        {
            if (string.IsNullOrEmpty(text) || 
                Uri.TryCreate(text, UriKind.Absolute, out _))
                return emptyPrediction;

            using var luisClient = CreateLuisRuntimeClient();

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
                    slotName: enviroment.GetVariable("LuisAppSlot", "staging"),
                    predictionRequest,
                    verbose: true,
                    showAllIntents: false,
                    log: true).ConfigureAwait(false));

            return predictionResponse.Prediction;
        }

        ILUISAuthoringClient CreateLuisAuthoringClient()
        {
            var credentials = new Authoring.ApiKeyServiceClientCredentials(
                enviroment.GetVariable("LuisAuthoringKey"));

            return new Authoring.LUISAuthoringClient(credentials, Array.Empty<DelegatingHandler>())
            {
                Endpoint = enviroment.GetVariable("LuisAuthoringEndpoint")
            };
        }

        ILUISRuntimeClient CreateLuisRuntimeClient()
        {
            var credentials = new Runtime.ApiKeyServiceClientCredentials(
                enviroment.GetVariable("LuisSubscriptionKey"));

            return new Runtime.LUISRuntimeClient(credentials, Array.Empty<DelegatingHandler>())
            {
                Endpoint = enviroment.GetVariable("LuisEndpoint")
            };
        }
    }

    interface ILanguageUnderstanding
    {
        Task AddUtteranceAsync(string? utterance, string? intent);
        Task<Prediction> PredictAsync(string text);
    }
}
