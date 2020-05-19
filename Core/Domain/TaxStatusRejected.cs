using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NosAyudamos
{
    public enum TaxStatusRejectedReason
    {
        HasEarnings,
        HighCategory,
    }

    public class TaxStatusRejected : DomainEvent
    {
        public TaxStatusRejected(string personId, string taxId, TaxStatusRejectedReason reason)
            => (PersonId, TaxId, Reason)
            = (personId, taxId, reason);

        public string PersonId { get; }
        public string TaxId { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TaxStatusRejectedReason Reason { get; }
    }
}
