using System.ComponentModel.DataAnnotations;

namespace NosAyudamos
{
    /// <summary>
    /// Maps between a users' phone and the system 
    /// phone number he's using to interact with it, 
    /// keyed by the person identifier, which is available 
    /// via <see cref="DomainEvent.SourceId"/> for all 
    /// domain events.
    /// </summary>
    class PersonMessagingMap
    {
        public PersonMessagingMap(string personId, string phoneNumber, string systemNumber)
            => (PersonId, PhoneNumber, SystemNumber)
            = (personId, phoneNumber, systemNumber);

        [Key]
        public string PersonId { get; }
        public string PhoneNumber { get; }
        public string SystemNumber { get; }
    }
}
