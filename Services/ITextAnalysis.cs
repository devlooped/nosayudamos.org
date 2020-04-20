using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;

namespace NosAyudamos
{
    public interface ITextAnalysis
    {
        Task<IEnumerable<string>> GetKeyPhrasesAsync(string? text);
        Task<IEnumerable<CategorizedEntity>> GetEntitiesAsync(string? text);
    }

    class TextAnalysis : ITextAnalysis
    {
        private readonly IEnvironment environment;

        public TextAnalysis(IEnvironment environment) => this.environment = environment;

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

            var response = await Task.Run(() => analyticsClient.RecognizeEntities(text)).ConfigureAwait(false);

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
}
