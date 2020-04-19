using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace NosAyudamos
{
    class Message
    {
        public string From { get; set; }
        public string Body { get; set; }
        public string To { get; set; }

        public Message(string from, string body, string to) => (From, Body, To) = (from, body, to);

        public static Message Create(string payload)
        {
            var values = payload.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0], x => x[1]);

            return new Message(values[nameof(From)], values[nameof(Body)], values[nameof(To)]);
        }
    }

    static class MessageExtensions
    {
        public static string SanitizeTo(this Message message)
        {
            return message.To.Replace("whatsapp:+", "", StringComparison.OrdinalIgnoreCase);
        }

        public static string SanitizeFom(this Message message)
        {
            return message.To.Replace("whatsapp:+", "", StringComparison.OrdinalIgnoreCase);
        }
    }
}
