using System;

namespace NosAyudamos.Events
{
    /// <summary>
    /// A message that was determined to contain text and not an image.
    /// </summary>
    public class TextMessageReceived : MessageEvent
    {
        public TextMessageReceived(string from, string to, string text) : base(from, to)
            => Text = text;

        public string? PersonId { get; set; }
        public string Text { get; }
    }

    /// <summary>
    /// A message that contains an image that can be retrieved 
    /// from the given <see cref="ImageUri"/>.
    /// </summary>
    public class ImageMessageReceived : MessageEvent
    {
        public ImageMessageReceived(string from, string to, Uri imageUri) : base(from, to)
            => ImageUri = imageUri;

        public string? PersonId { get; set; }
        public Uri ImageUri { get; }
    }

    /// <summary>
    /// A message that couldn't be understood or processed in a 
    /// meaningful way by the system. Might need manual intervention.
    /// </summary>
    public class UnknownMessageReceived : MessageReceived
    {
        public UnknownMessageReceived(string from, string to, string body) : base(from, to, body) { }

        public string? PersonId { get; set; }
    }

    public class MessageReceived : MessageEvent
    {
        public MessageReceived(string from, string to, string body) : base(from, to)
            => Body = body;

        public string Body { get; }
    }

    public class MessageSent : MessageEvent
    {
        public MessageSent(string from, string to, string body) : base(from, to)
            => Body = body;

        public string Body { get; }

        public string? PersonId { get; set; }
    }

    public abstract class MessageEvent
    {
        protected MessageEvent(string from, string to)
            => (From, To) = (from, to);

        public string From { get; }
        public string To { get; }
        public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
    }
}
