using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace NosAyudamos.Http
{
    class ChatApi
    {
        readonly Lazy<string> chatApiNumber;
        readonly IEventStreamAsync events;

        public ChatApi(IEnvironment enviroment, IEventStreamAsync events)
        {
            chatApiNumber = new Lazy<string>(() => enviroment.GetVariable("ChatApiNumber").TrimStart('+'));
            this.events = events;
        }

        [FunctionName("chat")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var payload = await reader.ReadToEndAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(payload);

            foreach (var message in json.GetProperty("messages").EnumerateArray())
            {
                var body = message.GetProperty("body").GetString();
                var from = message.GetProperty("author").GetString();
                var at = from.IndexOf('@', StringComparison.Ordinal);
                if (at != -1)
                    from = from.Substring(0, at);

                from = from.TrimStart('+');

                // Avoid reentrancy from our own messages.
                if (from == chatApiNumber.Value)
                    continue;

                await events.PushAsync(new NosAyudamos.MessageReceived(from, chatApiNumber.Value, body));
            }

            return new OkResult();
        }
    }
}
