using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Slack.Webhooks;
using System.Net.Http;

namespace NosAyudamos.Functions
{
    class Slack
    {
        const string ApiUrl = "https://slack.com/api/";

        readonly ISerializer serializer;
        readonly IEnvironment environment;
        readonly HttpClient http;
        readonly ILogger<Slack> logger;

        public Slack(ISerializer serializer, IEnvironment environment, HttpClient http, ILogger<Slack> logger) =>
            (this.serializer, this.environment, this.http, this.logger) = (serializer, environment, http, logger);

        [FunctionName("slack")]
        public async Task<IActionResult> IncomingAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack")] HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var payload = await reader.ReadToEndAsync();
            dynamic json = serializer.Deserialize<JObject>(payload);

#if DEBUG
            logger.LogInformation(((JObject)json).ToString(Newtonsoft.Json.Formatting.Indented));
#endif

            if (json.challenge != null)
            {
                return new OkObjectResult(json.challenge);
            }

            var message = SlackClient.DeserializeObject(payload);
            // We only process replies to a thread started by the initial DeadMessageReceived posted 
            // by the SlackForwarder.

            if (!string.IsNullOrEmpty(message.ThreadId))
            {
                using var client = new SlackClient(environment.GetVariable("SlackUsersWebHook"), httpClient: http);
                var token = environment.GetVariable("SlackToken");
                var response = await http.GetAsync($"{ApiUrl}/conversations.replies?token={token}&channel={message.Channel}&ts={message.ThreadId}&pretty=1")
                    .ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                //var thread = global::SlackAPI.response

                //var thread = (await http.GetAsync("GET https://slack.com/api/conversations.history?token=YOUR_TOKEN_HERE&channel=CONVERSATION_ID_HERE

            }

            System.Console.WriteLine();
            return new OkResult();
        }
    }
}
