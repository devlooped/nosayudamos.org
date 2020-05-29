using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    class AutomationActionProcessor : ISlackPayloadProcessor
    {
        readonly IEnvironment env;
        readonly IEventStreamAsync events;
        readonly HttpClient http;

        public AutomationActionProcessor(IEnvironment env, IEventStreamAsync events, HttpClient http)
            => (this.env, this.events, this.http)
            = (env, events, http);

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

            var userName = await http.ResolveUserAsync(env, userId);

            if (action == "pause")
            {
                await events.PushAsync(new AutomationPaused(sender, userName));
            }
            else if (action == "resume")
            {
                await events.PushAsync(new AutomationResumed(sender, userName));
            }
        }
    }
}
