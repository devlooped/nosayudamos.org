using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using Polly;
using Polly.Registry;

namespace NosAyudamos
{
    [Shared]
    class TextAnalysis : ITextAnalysis
    {
        private readonly IEnvironment environment;
        readonly IReadOnlyPolicyRegistry<string> registry;

        public TextAnalysis(IReadOnlyPolicyRegistry<string> registry, IEnvironment environment) => (this.registry, this.environment) = (registry, environment);

        public async Task<IEnumerable<string>> GetKeyPhrasesAsync(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            var analyticsClient = CreateAnalyticsClient();

            var response = await Task.Run(() => analyticsClient.ExtractKeyPhrases(text)).ConfigureAwait(false);

            return response.Value;
        }

        public async Task<IEnumerable<CategorizedEntity>> GetEntitiesAsync(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<CategorizedEntity>();

            var analyticsClient = CreateAnalyticsClient();

            var policy = registry.Get<IAsyncPolicy>("TextAnalysisPolicy");

            var response = await policy.ExecuteAsync(async () =>
                await Task.Run(() => analyticsClient.RecognizeEntities(text)).ConfigureAwait(false));

            return response.Value;
        }

        private TextAnalyticsClient CreateAnalyticsClient()
        {
            var credentials = new AzureKeyCredential(
                environment.GetVariable("TextAnalysisSubscriptionKey"));

            return new TextAnalyticsClient(
                new Uri(environment.GetVariable("TextAnalysisEndpoint")), credentials);
        }
    }

    interface ITextAnalysis
    {
        Task<IEnumerable<string>> GetKeyPhrasesAsync(string? text);
        Task<IEnumerable<CategorizedEntity>> GetEntitiesAsync(string? text);
    }
}
