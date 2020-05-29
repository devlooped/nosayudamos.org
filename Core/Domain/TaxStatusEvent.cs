using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NosAyudamos
{
    public abstract class TaxStatusEvent : DomainEvent
    {
        protected TaxStatusEvent(TaxId taxId) => TaxId = taxId;

        public TaxId TaxId { get; }
    }

    /// <summary>
    /// The tax status was manually approved by an approver, 
    /// regardless of the <see cref="TaxId"/> automated validation.
    /// </summary>
    public class TaxStatusApproved : DomainEvent
    {
        public TaxStatusApproved(string approver) => Approver = approver;

        public string Approver { get; }
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
