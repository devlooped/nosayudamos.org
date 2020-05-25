using System.ComponentModel.DataAnnotations;

namespace NosAyudamos
{
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

        [Key]
        public string Key { get; }
        public string ChannelId { get; }
        public string EventId { get; }
        public string ThreadId { get; }
        public string Text { get; }
        public string Payload { get; }
    }
}
