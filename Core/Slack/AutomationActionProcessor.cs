using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    class AutomationActionProcessor : ISlackPayloadProcessor
    {
        const string ApiUrl = "https://slack.com/api/users.info?user=";

        readonly IEnvironment environment;
        readonly IEventStreamAsync events;
        readonly HttpClient http;

        public AutomationActionProcessor(IEnvironment environment, IEventStreamAsync events, HttpClient http)
            => (this.environment, this.events, this.http)
            = (environment, events, http);

        public bool AppliesTo(JObject payload) =>
            (string?)payload["type"] == "block_actions" &&
            ((string?)payload.SelectToken("$.actions[0].value") == "pause" ||
             (string?)payload.SelectToken("$.actions[0].value") == "resume");

        public async Task ProcessAsync(JObject payload)
        {
            var action = (string)payload.SelectToken("$.actions[0].value")!;
            var userId = (string?)payload["user"]?["id"];
            var sender = payload.GetSender();

            if (userId == null || sender == null)
                return;

            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl + userId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", environment.GetVariable("SlackToken"));
            var response = await http.SendAsync(request);

            dynamic user = JObject.Parse(await response.Content.ReadAsStringAsync());
            var realName = (string?)user.user.real_name;

            if (action == "pause")
            {
                await events.PushAsync(new AutomationPaused(sender, realName ?? userId));
            }
            else if (action == "resume")
            {
                await events.PushAsync(new AutomationResumed(sender, realName ?? userId));
            }
        }
    }
}
