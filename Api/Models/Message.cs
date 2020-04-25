using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace NosAyudamos
{
    class Message
    {
        public string From { get; set; } = "";

        public string Body { get; set; } = "";

        public string To { get; set; } = "";

        public Message(string from, string body, string to) => (From, Body, To) = (from, body, to);

        private Message() { }

        public static Message Create(IDictionary<string, string> values)
        {
            var message = new Message();

            if (values.TryGetValue(nameof(From), out var from))
                message.From = from.Replace("whatsapp:", "", StringComparison.Ordinal).TrimStart('+').Trim();
            if (values.TryGetValue(nameof(To), out var to))
                message.To = to.Replace("whatsapp:", "", StringComparison.Ordinal).TrimStart('+').Trim();
            if (values.TryGetValue(nameof(Body), out var body))
                message.Body = body;

            return message;
        }
    }
}
