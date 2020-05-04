using System;

namespace NosAyudamos
{
    /// <summary>
    /// Prevents a type from being exported to the DI container automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    class NoExportAttribute : Attribute
    {
    }
}
