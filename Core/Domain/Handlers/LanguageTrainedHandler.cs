using System.Threading.Tasks;

namespace NosAyudamos
{
    class LanguageTrainedHandler : IEventHandler<LanguageTrained>
    {
        readonly ILanguageUnderstanding language;

        public LanguageTrainedHandler(ILanguageUnderstanding language) => this.language = language;

        public async Task HandleAsync(LanguageTrained e) => await language.AddUtteranceAsync(e.Utterance, e.Intent);
    }
}
