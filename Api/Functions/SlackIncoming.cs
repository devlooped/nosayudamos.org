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
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

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

            if (!req.Headers.TryGetValue("X-Slack-Signature", out var values) ||
                values.Count != 1 ||
                string.IsNullOrEmpty(values[0]) ||
                !req.Headers.TryGetValue("X-Slack-Request-Timestamp", out var timestamps) ||
                timestamps.Count != 1 ||
                string.IsNullOrEmpty(timestamps[0]))
                throw new ArgumentException("Slack signature is required.");

            // TODO: validate lifetime
            //if absolute_value(time.time() - timestamp) > 60 * 5:
            //    # The request timestamp is more than five minutes from local time.
            //    # It could be a replay attack, so let's ignore it.
            //    return

            var expectedSignature = Encoding.UTF8.GetBytes(values[0]);
            var signedData = "v0:" + timestamps[0] + ":" + payload;
            var signingSecret = environment.GetVariable("SlackSigningSecret");
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
            var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedData));
            // Something is off with the way we get bytes and compare signature :(
            // TODO: pga
            //if (!expectedSignature.SequenceEqual(signature))
            //    throw new ArgumentException("Invalid slack Slack signature is required.");

            dynamic json = serializer.Deserialize<JObject>(System.Net.WebUtility.UrlDecode(payload.Substring(8)));

            if (json.challenge != null)
            {
                return new OkObjectResult(json.challenge);
            }

            string? intent = json.actions?[0]?.value;
            string? message = json.message?.blocks?[2]?.text?.text;
            if (message != null && message.Trim().StartsWith("&gt;", StringComparison.Ordinal))
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
