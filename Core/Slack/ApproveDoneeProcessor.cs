using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Slack.Webhooks;

namespace NosAyudamos.Slack
{
    class ApproveDoneeProcessor : ISlackPayloadProcessor
    {
        readonly IEnvironment env;
        readonly IPersonRepository peopleRepo;
        readonly IEventStreamAsync events;
        readonly HttpClient http;

        public ApproveDoneeProcessor(IEnvironment env,
            IPersonRepository peopleRepo,
            IEventStreamAsync events, HttpClient http)
            => (this.env, this.peopleRepo, this.events, this.http)
            = (env, peopleRepo, events, http);

        public bool AppliesTo(JObject payload) =>
            (string?)payload["type"] == "block_actions" &&
            ((string?)payload.SelectToken("$.actions[0].value") == "approve");

        public async Task ProcessAsync(JObject payload)
        {
            var userId = (string?)payload["user"]?["id"];
            var sender = payload.GetSender();

            if (userId == null || sender == null)
                return;

            var userName = await http.ResolveUserAsync(env, userId);
            var person = await peopleRepo.FindAsync(sender, readOnly: false);

            if (person == null)
                return;

            person.ApproveTaxStatus(userName);
            await peopleRepo.PutAsync(person);

            await events.PushAsync(new SlackMessageSent(sender, new SlackMessage
            {
                Text = $"Ok <@{userId}>, user has been approved",
            }.AsJson()));
        }
    }
}
