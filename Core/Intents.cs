namespace NosAyudamos
{
    public static class Intents
    {
        public const string Help = nameof(Help);
        public const string Donate = nameof(Donate);
        public const string Instructions = nameof(Instructions);

        public static class Utilities
        {
            public const string Help = nameof(Utilities) + "." + nameof(Help);
        }
    }
}
