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
    }
}
