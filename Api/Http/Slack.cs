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
using System.Net.Http.Headers;


namespace NosAyudamos.Http
{
    class Slack
    {
        const string ApiUrl = "https://slack.com/api/";
        const string UserInfoUrl = ApiUrl + "users.info?user=";

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

        [FunctionName("slack-interaction")]
        public async Task<IActionResult> InteractionAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/interaction")] HttpRequest req)
        {
            var payload = await GetValidatedPayloadAsync(req);

            dynamic json = serializer.Deserialize<JObject>(System.Net.WebUtility.UrlDecode(payload.Substring(8)));

            if (json.challenge != null)
            {
                return new OkObjectResult(json.challenge);
            }

            string? action = json.actions?[0]?.value;
            string? message = json.message?.blocks?[2]?.text?.text;
            if (message != null && message.Trim().StartsWith("&gt;", StringComparison.Ordinal))
                message = message.Trim().Substring(4);

            if (message != null)
                message = message.Trim();

            if (action == "donate" || action == "help")
            {
                if (message != null)
                    await events.PushAsync(new LanguageTrained(action, message));

                return new OkResult();
            }

            string? from = json.message?.blocks?[1]?.fields?[0]?.text;
            if (string.IsNullOrEmpty(from))
                return new OkResult();

            from = from.Substring(from.LastIndexOf(':') + 1).Trim();

            if (action == "retry")
            {
                var map = await phoneRepo.GetAsync(from);
                if (map != null && message != null)
                    await events.PushAsync(new MessageReceived(from, map.SystemNumber, message));

                return new OkResult();
            }

            var userId = (string)json.user.id;
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoUrl + userId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", environment.GetVariable("SlackToken"));
            var response = await http.SendAsync(request);

            json = JObject.Parse(await response.Content.ReadAsStringAsync());
            userId = (string)json.user.real_name;
            if (action == "pause")
            {
                await events.PushAsync(new AutomationPaused(from, userId));
            }
            else if (action == "resume")
            {
                await events.PushAsync(new AutomationResumed(from, userId));
            }

            return new OkResult();
        }

        [FunctionName("slack-message")]
        public async Task<IActionResult> ReceiveAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/message")] HttpRequest req)
        {
            var payload = await GetValidatedPayloadAsync(req);
            dynamic json = serializer.Deserialize<JObject>(payload);

            if (json.challenge != null)
            {
                return new OkObjectResult(json.challenge);
            }

            string channelId = json["event"].channel;
            string eventId = json["event"].event_ts;
            string threadId = json["event"].thread_ts;
            string text = json["event"].text;

            // We only process thread replies
            if (!string.IsNullOrEmpty(threadId) && !string.IsNullOrEmpty(text))
                await events.PushAsync(new SlackEventReceived(channelId, eventId, threadId, text, payload));

            return new OkResult();
        }

        async Task<string> GetValidatedPayloadAsync(HttpRequest req)
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
                throw new ArgumentException("Invalid Slack signature.");

            return payload;
        }
    }
}
