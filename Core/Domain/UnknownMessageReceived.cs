namespace NosAyudamos
{
    /// <summary>
    /// A message that couldn't be understood or processed in a 
    /// meaningful way by the system. Might need manual intervention.
    /// </summary>
    public class UnknownMessageReceived : MessageEvent
    {
        public UnknownMessageReceived(string from, string body) : base(from)
            => Body = body;

        public string Body { get; }

        /// <summary>
        /// Optionally, the person that sent it, if it could be 
        /// identified as a registered user at all.
        /// </summary>
        public string? PersonId { get; set; }
    }
}
