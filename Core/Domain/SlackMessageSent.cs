namespace NosAyudamos
{
    /// <summary>
    /// Notifies interested listeners that a message to Slack was sent (to be 
    /// delivered to Slack by an actual listener of this event).
    /// </summary>
    public class SlackMessageSent
    {
        public SlackMessageSent(string phoneNumber, string messageJson)
            => (PhoneNumber, MessageJson)
            = (phoneNumber, messageJson);

        /// <summary>
        /// The user's phone number that triggered this message.
        /// </summary>
        public string PhoneNumber { get; }
        /// <summary>
        /// The payload to post to the Slack webhook.
        /// </summary>
        public string MessageJson { get; }
    }
}
