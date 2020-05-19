namespace NosAyudamos
{
    public enum TaxIdKind
    {
        /// <summary>
        /// We don't know what kind of tax registration the person has.
        /// </summary>
        Unknown,
        /// <summary>
        /// We determined the person does not have a tax registration at all, 
        /// of any kind (CUIT nor CUIL).
        /// </summary>
        None,
        /// <summary>
        /// Person has a CUIT.
        /// </summary>
        CUIT,
        /// <summary>
        /// Person has a CUIL.
        /// </summary>
        CUIL,
    }
}
