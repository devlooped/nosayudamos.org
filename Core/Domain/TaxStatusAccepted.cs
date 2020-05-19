namespace NosAyudamos
{
    public class TaxStatusAccepted : DomainEvent
    {
        public TaxStatusAccepted(string taxId, TaxIdKind kind, string? category = default)
            => (TaxId, Kind, Category)
            = (taxId, kind, category);

        public string TaxId { get; }
        public TaxIdKind Kind { get; }
        public string? Category { get; }
    }
}
