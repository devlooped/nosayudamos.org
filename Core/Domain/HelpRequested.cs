namespace NosAyudamos
{
    class HelpRequested : DomainEvent
    {
        public HelpRequested(double amount, string? description)
            => (Amount, Description)
            = (amount, description);

        public double Amount { get; }
        public string? Description { get; }
    }
}
