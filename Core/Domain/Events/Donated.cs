namespace NosAyudamos
{
    public class Donated : DomainEvent
    {
        public Donated(int amount) => Amount = amount;

        public int Amount { get; }
    }
}
