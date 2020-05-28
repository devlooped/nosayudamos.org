using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Globalization;
using NosAyudamos.Slack;
using System.Collections.Generic;

namespace NosAyudamos.Http
{
    class Slack
    {
        readonly IServiceProvider services;
        readonly ISerializer serializer;
        readonly IEnvironment env;

        public Slack(IServiceProvider services, ISerializer serializer, IEnvironment env)
            => (this.services, this.serializer, this.env)
            = (services, serializer, env);

        [FunctionName("slack-interaction")]
        public async Task<IActionResult> InteractionAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/interaction")] HttpRequest req)
        {
            var payload = await GetValidatedPayloadAsync(req);
            var json = serializer.Deserialize<JObject>(System.Net.WebUtility.UrlDecode(payload.Substring(8)));

            if (json["challenge"] != null)
                return new OkObjectResult((string)json["challenge"]!);

            foreach (var processor in services
                .GetRequiredService<IEnumerable<ISlackPayloadProcessor>>()
                .Where(x => x.AppliesTo(json)))
            {
                await processor.ProcessAsync(json);
            }

            return new OkResult();
        }

        [FunctionName("slack-message")]
        public async Task<IActionResult> MessageAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/message")] HttpRequest req)
        {
            var payload = await GetValidatedPayloadAsync(req);
            var json = serializer.Deserialize<JObject>(payload);

            if (json["challenge"] != null)
                return new OkObjectResult((string)json["challenge"]!);

            foreach (var processor in services
                .GetRequiredService<IEnumerable<ISlackPayloadProcessor>>()
                .Where(x => x.AppliesTo(json)))
            {
                await processor.ProcessAsync(json);
            }

            return new OkResult();
        }

        [FunctionName("slack-command")]
        public async Task<IActionResult> CommandAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/command")] HttpRequest req)
        {
            var payload = await GetValidatedPayloadAsync(req);
            var json = serializer.Deserialize<JObject>(payload);

            if (json["challenge"] != null)
                return new OkObjectResult((string)json["challenge"]!);

            foreach (var processor in services
                .GetRequiredService<IEnumerable<ISlackPayloadProcessor>>()
                .Where(x => x.AppliesTo(json)))
            {
                await processor.ProcessAsync(json);
            }

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
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(env.GetVariable("SlackSigningSecret")));
            var signature = "v0=" + hmac.ComputeHash(signedData).Aggregate("", (s, b) => s + b.ToString("x2", CultureInfo.CurrentCulture));

            if (!expectedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid Slack signature.");

            return payload;
        }
    }
}
