using System;

namespace NosAyudamos
{
    /// <summary>
    /// Flags the property to use as the table storage row key 
    /// when storing the annotated type using the <see cref="EntityRepository{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class RowKeyAttribute : Attribute
    {
    }
}
