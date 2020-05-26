using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NosAyudamos.Slack;

namespace NosAyudamos
{
    class SlackEventReceivedHandler : IEventHandler<SlackEventReceived>
    {
        const string ApiUrl = "https://slack.com/api/";

        readonly IEnvironment environment;
        readonly IEventStreamAsync events;
        readonly IEntityRepository<SlackEventReceived> repository;
        readonly HttpClient http;

        public SlackEventReceivedHandler(
            IEnvironment environment, IEventStreamAsync events, 
            IEntityRepository<SlackEventReceived> repository, HttpClient http)
            => (this.environment, this.events, this.repository, this.http)
            = (environment, events, repository, http);

        public async Task HandleAsync(SlackEventReceived e)
        {
            // We only process thread replies
            if (!string.IsNullOrEmpty(e.ThreadId))
            {
                var entity = await repository.GetAsync(e.Key);
                if (entity != null)
                    return;

                // Save right-away. If this method throws for whatever reason, 
                // EventGrid would still be retrying.
                await repository.PutAsync(e);


                using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiUrl}/conversations.replies?channel={e.ChannelId}&ts={e.ThreadId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", environment.GetVariable("SlackToken"));
                var response = await http.SendAsync(request).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JsonConvert.DeserializeObject<JObject>(body);

                string? from = json.SelectString("$.messages[0].blocks[?(@.block_id == 'sender')].fields[0].text");

                if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(e.Text))
                    await events.PushAsync(new MessageSent(from.Substring(from.LastIndexOf(':') + 1).Trim(), e.Text));

                await repository.PutAsync(e);
            }
        }
    }
}
