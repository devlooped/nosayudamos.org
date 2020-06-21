using Azure.AI.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

namespace NosAyudamos
{
    class TextAnalysis
    {
        public TextAnalysis(Prediction prediction, CategorizedEntity[] entities, string[] keyPhrases)
            => (Prediction, Entities, KeyPhrases)
            = (prediction, entities, keyPhrases);

        public Prediction Prediction { get; }
        public CategorizedEntity[] Entities { get; }
        public string[] KeyPhrases { get; }
    }
}
