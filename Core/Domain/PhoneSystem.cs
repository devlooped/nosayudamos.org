using System.ComponentModel.DataAnnotations;

namespace NosAyudamos
{
    /// <summary>
    /// Maps the phone system used by a user, by phone number
    /// </summary>
    public class PhoneSystem
    {
        public PhoneSystem(string userNumber, string systemNumber)
        {
            UserNumber = userNumber;
            SystemNumber = systemNumber;
        }

        [Key]
        public string UserNumber { get; set; }
        public string SystemNumber { get; set; }

        /// <summary>
        /// Allows pausing the regular message processing for interactions 
        /// with this phone number. This allows rich follow-up via Slack 
        /// without interferring with the normal processing of incoming 
        /// messages.
        /// </summary>
        public bool? AutomationPaused { get; set; }
    }
}
