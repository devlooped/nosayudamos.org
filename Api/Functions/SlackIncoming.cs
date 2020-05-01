using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Merq;
using NosAyudamos.Events;

namespace NosAyudamos.Functions
{
    class SlackIncoming
    {
        const string ApiUrl = "https://slack.com/api/";

        readonly ISerializer serializer;
        readonly IEnvironment environment;
        readonly IEventStream events;
        readonly HttpClient http;
        readonly ILogger<SlackIncoming> logger;

        public SlackIncoming(ISerializer serializer, IEnvironment environment, IEventStream events, HttpClient http, ILogger<SlackIncoming> logger) =>
            (this.serializer, this.environment, this.events, this.http, this.logger) = (serializer, environment, events, http, logger);

        [FunctionName("slack_incoming")]
        public async Task<IActionResult> ReceiveAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var payload = await reader.ReadToEndAsync();
            dynamic json = serializer.Deserialize<JObject>(payload);

            if (json.challenge != null)
            {
                return new OkObjectResult(json.challenge);
            }

            // We only process replies to a thread started by the initial UnknownMessageReceived posted 
            // by the SlackForwarder.
            string threadId = json["event"].thread_ts;

            if (!string.IsNullOrEmpty(threadId))
            {
                // Keep the original response text.
                string text = json["event"].text;

                var token = environment.GetVariable("SlackToken");
                var response = await http.GetAsync($"{ApiUrl}/conversations.replies?token={token}&channel={json["event"].channel}&ts={threadId}&pretty=1")
                    .ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                json = serializer.Deserialize<JObject>(body);

                if (json.messages?[0]?.attachments?[0]?.fields == null)
                    return new OkResult();

                dynamic fields = json.messages[0].attachments[0].fields;

                string? from = fields[0]?.value;
                string? to = fields[1]?.value;

                if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
                    events.Push(new MessageSent(to, from, text));
            }

            System.Console.WriteLine();
            return new OkResult();
        }
    }
}
