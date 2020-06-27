namespace NosAyudamos
{
    public class PhoneNumberUpdated : DomainEvent
    {
        public PhoneNumberUpdated(string oldNumber, string newNumber)
            => (OldNumber, NewNumber)
            = (oldNumber, newNumber);

        public string OldNumber { get; }

        public string NewNumber { get; }
    }
}
