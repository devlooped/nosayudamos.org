namespace NosAyudamos
{
    /// <summary>
    /// Phone directory entry for numbers used by our users 
    /// (i.e. for contacting the system via WhatsApp or Twilio).
    /// </summary>
    public class PhoneEntry
    {
        public PhoneEntry(string userNumber, string systemNumber)
        {
            UserNumber = userNumber;
            SystemNumber = systemNumber;
        }

        /// <summary>
        /// User's phone number.
        /// </summary>
        [RowKey]
        public string UserNumber { get; set; }

        /// <summary>
        /// System phone number most recently used by the user.
        /// </summary>
        public string SystemNumber { get; set; }

        /// <summary>
        /// Allows pausing the regular message processing for interactions 
        /// with this phone number. This allows rich follow-up via Slack 
        /// without interferring with the normal processing of incoming 
        /// messages.
        /// </summary>
        public bool? AutomationPaused { get; set; }

        /// <summary>
        /// The user's <see cref="Role"/>, typically expressed in the 
        /// initial interactions as <see cref="Intents.Donate"/> or 
        /// <see cref="Intents.Help"/>.
        /// </summary>
        public Role? Role { get; set; }
    }
}
