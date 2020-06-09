namespace NosAyudamos
{
    public abstract class FundsEvent : DomainEvent
    {
        public FundsEvent(double amount, string person)
            => (Amount, Person)
            = (amount, person);

        public double Amount { get; }
        public string Person { get; }
    }

    public class FundsAdded : FundsEvent
    {
        public FundsAdded(double amount, string person) : base(amount, person) { }
    }

    public class FundsAssigned : FundsEvent
    {
        public FundsAssigned(double amount, string person) : base(amount, person) { }
    }

    public class FundsRequested : FundsEvent
    {
        public FundsRequested(double amount, string person) : base(amount, person) { }
    }

    public class FundsRequestReplaced : FundsEvent
    {
        public FundsRequestReplaced(double amount, string person) : base(amount, person) { }
    }

}
