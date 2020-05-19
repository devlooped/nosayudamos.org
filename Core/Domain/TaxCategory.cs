namespace NosAyudamos
{
    public enum TaxCategory
    {
        /// <summary>
        /// We don't know the tax category the person has, if any.
        /// </summary>
        Unknown,
        /// <summary>
        /// The person is registered to a tax regime that does not 
        /// have simplified categories (aka 'Monotributo').
        /// </summary>
        NotApplicable,
        A,
        B,
        C,
        D,
    }
}
