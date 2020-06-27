namespace NosAyudamos
{
    /// <summary>
    /// The tax status of a <see cref="Donee"/>.
    /// </summary>
    public enum TaxStatus
    {
        /// <summary>
        /// Tax status is not yet known.
        /// </summary>
        Unknown,
        /// <summary>
        /// Tax status was explicitly approved by an approver.
        /// </summary>
        Approved,
        /// <summary>
        /// Tax status was validated automatically based on 
        /// rules applied to the <see cref="TaxIdKind"/> and 
        /// <see cref="TaxCategory"/> of the user.
        /// </summary>
        Validated,
        /// <summary>
        /// Tax status was rejected automatically based on 
        /// rules applied to the <see cref="TaxIdKind"/> and 
        /// <see cref="TaxCategory"/> of the user.
        /// </summary>
        Rejected,
    }
}
