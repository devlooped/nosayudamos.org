using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    class RetryMessageProcessor : ISlackPayloadProcessor
    {
        readonly IEventStreamAsync events;
        readonly IEntityRepository<PhoneSystem> repository;

        public RetryMessageProcessor(IEventStreamAsync events, IEntityRepository<PhoneSystem> repository)
            => (this.events, this.repository)
            = (events, repository);

        public bool AppliesTo(JObject payload) =>
            (string?)payload["type"] == "block_actions" &&
            payload.SelectString("$.actions[0].value") == "retry" &&
            payload.GetSender() != null &&
            payload.SelectString("$.message.blocks[?(@.block_id == 'body')].text.text") != null;

        public async Task ProcessAsync(JObject payload)
        {
            var sender = payload.GetSender()!;
            var message = payload.SelectString("$.message.blocks[?(@.block_id == 'body')].text.text")!.Trim();

            var map = await repository.GetAsync(sender);
            if (map != null && message != null)
                await events.PushAsync(new MessageReceived(sender, map.SystemNumber, message));
        }
    }
}
