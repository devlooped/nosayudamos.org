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
    /// regardless of the <see cref="TaxId"/> automated validation, 
    /// or automatically approved in the case of a donor.
    /// </summary>
    public class TaxStatusApproved : DomainEvent
    {
        public TaxStatusApproved(string approver) => Approver = approver;

        public string Approver { get; }
    }

    /// <summary>
    /// Tax status was validated automatically based on rules 
    /// according to the <see cref="TaxId.Category"/> and 
    /// <see cref="TaxId.Kind"/>.
    /// </summary>
    public class TaxStatusValidated : TaxStatusEvent
    {
        public TaxStatusValidated(TaxId taxId) : base(taxId) { }
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
