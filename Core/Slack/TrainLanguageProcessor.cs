using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    class TrainLanguageProcessor : ISlackPayloadProcessor
    {
        readonly IEventStreamAsync events;

        public TrainLanguageProcessor(IEventStreamAsync events) => this.events = events;

        public bool AppliesTo(JObject payload) =>
            (string?)payload["type"] == "block_actions" &&
            (payload.SelectString("$.actions[0].value") == "donate" ||
             payload.SelectString("$.actions[0].value") == "help") && 
            payload.SelectString("$.message.blocks[?(@.block_id == 'body')].text.text") != null;

        public async Task ProcessAsync(JObject payload)
        {
            var action = payload.SelectString("$.actions[0].value")!;
            var utterance = payload.SelectString("$.message.blocks[?(@.block_id == 'body')].text.text")!.Trim();

            await events.PushAsync(new LanguageTrained(action, utterance));
        }
    }
}
