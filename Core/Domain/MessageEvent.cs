using System;

namespace NosAyudamos
{
    public abstract class MessageEvent
    {
        protected MessageEvent(string from, string to)
            => (From, To) = (from, to);

        public string From { get; }
        public string To { get; }
        public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
    }
}
