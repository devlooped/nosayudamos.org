using System.Linq;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

namespace NosAyudamos
{
    internal static class Intents
    {
        public static bool IsIntent(this Prediction prediction, string intent)
            => prediction.TopIntent == intent &&
                prediction.Intents.TryGetValue(intent, out var value) &&
                value.Score >= 0.85;

        public static bool IsIntent(this Prediction prediction, params string[] intents)
            => intents.Any(intent => prediction.IsIntent(intent));

        public const string Help = nameof(Help);
        public const string Donate = nameof(Donate);
        public const string Instructions = nameof(Instructions);

        public static class Utilities
        {
            public const string Help = nameof(Utilities) + "." + nameof(Help);
        }
    }
}
