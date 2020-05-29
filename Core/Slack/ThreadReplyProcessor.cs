using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    /// <summary>
    /// Processes replies to messages from within a thread in Slack, typically to 
    /// conduct direct conversations with users.
    /// </summary>
    class ThreadReplyProcessor : ISlackPayloadProcessor
    {
        readonly IEventStreamAsync events;

        public ThreadReplyProcessor(IEventStreamAsync events) => this.events = events;

        public bool AppliesTo(JObject payload) =>
            (string?)payload["type"] == "event_callback" &&
            // Only process actual client messages (discards bot messages, which don't have one)
            payload.SelectToken("$.event.client_msg_id") != null &&
            // Only process threaded replies, where we can locate the original sender
            payload.SelectToken("$.event.thread_ts") != null &&
            // Only if there's some text we can send to the user
            payload.SelectToken("$.event.text") != null;

        public async Task ProcessAsync(JObject payload)
        {
            dynamic json = payload;

            await events.PushAsync(new SlackEventReceived(
                (string)json["event"].channel,
                (string)json["event"].event_ts,
                (string)json["event"].thread_ts,
                (string)json["event"].text,
                payload.ToString()));
        }
    }
}
