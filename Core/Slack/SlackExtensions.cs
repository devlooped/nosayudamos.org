using System;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    static class SlackExtensions
    {
        public static string? SelectString(this JObject json, string path)
            => (string?)json.SelectToken(path);

        public static string? GetSender(this JObject json)
        {
            var sender = json.SelectString("$.message.blocks[?(@.block_id == 'sender')].fields[0].text");
            if (sender == null)
                sender = json.SelectString("$.view.blocks[?(@.block_id == 'sender')].text.text");

            if (!string.IsNullOrEmpty(sender) && sender.IndexOf(':', StringComparison.Ordinal) != -1)
                sender = sender.Substring(sender.LastIndexOf(':') + 1);

            return sender?.Trim();
        }
    }
}
