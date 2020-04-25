using System;

namespace NosAyudamos.Events
{
    public class TextMessageReceived
    {
        public TextMessageReceived(string from, string to, string text)
            => (From, To, Text) = (from, to, text);

        public string? PersonId { get; set; }
        public string From { get; }
        public string To { get; }
        public string Text { get; }
    }

    public class ImageMessageReceived
    {
        public ImageMessageReceived(string from, string to, Uri imageUri)
            => (From, To, ImageUri) = (from, to, imageUri);

        public string? PersonId { get; set; }
        public string From { get; }
        public string To { get; }
        public Uri ImageUri { get; }
    }

    public class MessageReceived : MessageEvent
    {
        public MessageReceived(string from, string to, string body) : base(from, to, body) { }
    }

    public class MessageSent : MessageEvent
    {
        public MessageSent(string from, string to, string body) : base(from, to, body) { }

        public string? PersonId { get; set; }
    }

    public abstract class MessageEvent
    {
        protected MessageEvent(string from, string to, string body)
            => (From, To, Body) = (from, to, body);

        public string From { get; }
        public string To { get; }
        public string Body { get; }
    }
}
