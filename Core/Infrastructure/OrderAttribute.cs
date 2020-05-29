using System;

namespace NosAyudamos
{
    /// <summary>
    /// Prevents a type from being exported to the DI container automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    class OrderAttribute : Attribute
    {
        public OrderAttribute(int order) => Order = order;

        public int Order { get; }
    }
}
