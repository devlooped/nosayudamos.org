﻿namespace NosAyudamos
{
    /// <summary>
    /// A message that couldn't be understood or processed in a 
    /// meaningful way by the system. Might need manual intervention.
    /// </summary>
    public class UnknownMessageReceived : MessageReceived
    {
        public UnknownMessageReceived(string from, string to, string body) : base(from, to, body) { }

        /// <summary>
        /// Optionally, the person that sent it, if it could be 
        /// identified as a registered user at all.
        /// </summary>
        public string? PersonId { get; set; }
    }
}
