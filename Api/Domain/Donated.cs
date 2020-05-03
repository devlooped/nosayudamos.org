namespace NosAyudamos
{
    public class Donated : DomainEvent
    {
        public Donated(double amount) => Amount = amount;

        public double Amount { get; }
    }
}
