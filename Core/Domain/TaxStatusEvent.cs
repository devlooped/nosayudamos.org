using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NosAyudamos
{
    public abstract class TaxStatusEvent : DomainEvent
    {
        protected TaxStatusEvent(TaxId taxId) => TaxId = taxId;

        public TaxId TaxId { get; }
    }

    public class TaxStatusAccepted : TaxStatusEvent
    {
        public TaxStatusAccepted(TaxId taxId) : base(taxId) { }
    }

    public enum TaxStatusRejectedReason { HasIncomeTax, NotApplicable, HighCategory }

    public class TaxStatusRejected : TaxStatusEvent
    {
        public TaxStatusRejected(TaxId taxId, TaxStatusRejectedReason reason)
            : base(taxId)
            => Reason = reason;

        [JsonConverter(typeof(StringEnumConverter))]
        public TaxStatusRejectedReason Reason { get; }
    }
}
