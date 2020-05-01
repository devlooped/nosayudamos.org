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
using System.Globalization;

namespace NosAyudamos.Functions
{
    class SlackIncoming
    {
        const string ApiUrl = "https://slack.com/api/";

        readonly ISerializer serializer;
        readonly IEnvironment environment;
        readonly IEventStream events;
        readonly ILanguageUnderstanding language;
        readonly HttpClient http;
        readonly ILogger<SlackIncoming> logger;

        public SlackIncoming(ISerializer serializer, IEnvironment environment, IEventStream events, ILanguageUnderstanding language, HttpClient http, ILogger<SlackIncoming> logger) =>
            (this.serializer, this.environment, this.events, this.language, this.http, this.logger) = (serializer, environment, events, language, http, logger);

        [FunctionName("slack_interaction")]
        public async Task<IActionResult> InteractionAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/interaction")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var payload = await reader.ReadToEndAsync();
            dynamic json = serializer.Deserialize<JObject>(System.Net.WebUtility.UrlDecode(payload.Substring(8)));

            if (json.challenge != null)
            {
                return new OkObjectResult(json.challenge);
            }

            string? intent = json.actions?[0]?.value;
            string? message = json.message?.blocks?[2]?.text?.text;
            if (message != null && message.Trim().StartsWith("&gt;", System.StringComparison.Ordinal))
                message = message.Trim().Substring(4);

            if (message != null)
                message = message.Trim();

            // TODO: uncomment when new API is merged.
            //await language.AddUtteranceAsync(message, intent);
            logger.LogInformation("Training intent '{intent}' with new phrase: {phrase}", intent, message);

            return new OkResult();
        }

        [FunctionName("slack_incoming")]
        public async Task<IActionResult> ReceiveAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/message")] HttpRequest req)
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
                string? text = json["event"]?.text;
                if (string.IsNullOrEmpty(text))
                    return new OkResult();

                var token = environment.GetVariable("SlackToken");
                var response = await http.GetAsync($"{ApiUrl}/conversations.replies?token={token}&channel={json["event"].channel}&ts={threadId}&pretty=1")
                    .ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                json = serializer.Deserialize<JObject>(body);

                string? from = json.messages?[0]?.blocks?[1]?.fields?[0]?.text;
                string? to = json.messages?[0]?.blocks?[1]?.fields?[1]?.text;

                if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to) && !string.IsNullOrEmpty(text))
                    events.Push(new MessageSent(to.Substring(to.LastIndexOf(':') + 1).Trim(), from.Substring(from.LastIndexOf(':') + 1).Trim(), text));
            }

            return new OkResult();
        }
    }
}
