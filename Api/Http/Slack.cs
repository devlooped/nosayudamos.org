using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Globalization;

namespace NosAyudamos.Http
{
    class Slack
    {
        const string ApiUrl = "https://slack.com/api/";

        readonly ISerializer serializer;
        readonly IEnvironment environment;
        readonly IEventStreamAsync events;
        readonly ILanguageUnderstanding language;
        readonly IEntityRepository<PhoneSystem> phoneRepo;
        readonly HttpClient http;
        readonly ILogger<Slack> logger;
        readonly MessageReceivedHandler handler;

        public Slack(
            ISerializer serializer, IEnvironment environment, 
            IEventStreamAsync events, ILanguageUnderstanding language,
            IEntityRepository<PhoneSystem> phoneRepo,
            HttpClient http, MessageReceivedHandler handler, ILogger<Slack> logger) 
            => (this.serializer, this.environment, this.events, this.language, this.phoneRepo, this.http, this.handler, this.logger) 
            = (serializer, environment, events, language, phoneRepo, http, handler, logger);

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

            var expectedSignature = values[0];
            var signedData = Encoding.UTF8.GetBytes("v0:" + timestamps[0] + ":" + payload);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(environment.GetVariable("SlackSigningSecret")));
            var signature = "v0=" + hmac.ComputeHash(signedData).Aggregate("", (s, b) => s + b.ToString("x2", CultureInfo.CurrentCulture));

            if (!expectedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid Slack signature.");
            }

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

            if (intent == "retry")
            {
                string? from = json.message?.blocks?[1]?.fields?[0]?.text;
                if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(message))
                {
                    from = from.Substring(from.LastIndexOf(':') + 1).Trim();
                    var map = await phoneRepo.GetAsync(from);
                    if (map != null)
                        await handler.HandleAsync(new MessageReceived(from, map.SystemNumber, message));
                }
            }
            else
            {
                // TODO: uncomment when new API is merged.
                logger.LogInformation("Training intent '{intent}' with new phrase: {phrase}", intent, message);
                await language.AddUtteranceAsync(message, intent);
            }

            return new OkResult();
        }

        [FunctionName("slack_message")]
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

                if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(text))
                    await events.PushAsync(new NosAyudamos.MessageSent(from.Substring(from.LastIndexOf(':') + 1).Trim(), text));
            }

            return new OkResult();
        }
    }
}
