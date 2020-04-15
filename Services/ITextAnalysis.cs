using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;

namespace NosAyudamos
{
    public interface ITextAnalysis
    {
        Task<IEnumerable<string>> GetKeyPhrasesAsync(string text);
        Task<IEnumerable<CategorizedEntity>> GetentitiesAsync(string text);
    }

    public class TextAnalysis : ITextAnalysis
    {
        private readonly IEnviroment enviroment;

        public TextAnalysis(IEnviroment env)
        {
            enviroment = env;
        }

        public async Task<IEnumerable<string>> GetKeyPhrasesAsync(string text)
        {
            var analyticsClient = CreateAnalyticsClient();

            var response = await Task.Run(() => analyticsClient.ExtractKeyPhrases(text)).ConfigureAwait(false);

            return response.Value;
        }

        public async Task<IEnumerable<CategorizedEntity>> GetentitiesAsync(string text)
        {
            var analyticsClient = CreateAnalyticsClient();

            var response = await Task.Run(() => analyticsClient.RecognizeEntities(text)).ConfigureAwait(false);

            return response.Value;
        }

        private TextAnalyticsClient CreateAnalyticsClient()
        {
            var credentials = new AzureKeyCredential(
                enviroment.GetVariable("TextAnalysisSubscriptionKey"));

            return new TextAnalyticsClient(
                new Uri(enviroment.GetVariable("TextAnalysisEndpoint")), credentials);
        }
    }
}