namespace NosAyudamos
{
    public class AutomationPaused
    {
        public AutomationPaused(string phoneNumber, string by) 
            => (PhoneNumber, By)
            = (phoneNumber, by);

        public string By { get; }
        public string PhoneNumber { get; }
    }

    public class AutomationResumed
    {
        public AutomationResumed(string phoneNumber, string by)
            => (PhoneNumber, By)
            = (phoneNumber, by);

        public string By { get; }
        public string PhoneNumber { get; }
    }
}
