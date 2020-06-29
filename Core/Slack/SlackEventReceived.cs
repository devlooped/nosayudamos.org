using System.ComponentModel.DataAnnotations;

namespace NosAyudamos
{
    /// <summary>
    /// Represents a Slack Event Subscription callback, turned into an entity so that 
    /// it can be persisted to detect duplicate processing and sent to the event grid 
    /// to get resilient re-processing if something fails.
    /// </summary>
    public class SlackEventReceived
    {
        public SlackEventReceived(string channelId, string eventId, string threadId, string text, string payload)
        {
            Key = channelId + "-" + eventId;
            ChannelId = channelId;
            EventId = eventId;
            ThreadId = threadId;
            Text = text;
            Payload = payload;
        }

        [RowKey]
        public string Key { get; }
        public string ChannelId { get; }
        public string EventId { get; }
        public string ThreadId { get; }
        public string Text { get; }
        public string Payload { get; }
    }
}
