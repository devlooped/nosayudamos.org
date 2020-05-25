namespace NosAyudamos
{
    public class LanguageTrained
    {
        public LanguageTrained(string intent, string utterance)
            => (Intent, Utterance)
            = (intent, utterance);

        public string Intent { get; }
        public string Utterance { get; }
    }
}
