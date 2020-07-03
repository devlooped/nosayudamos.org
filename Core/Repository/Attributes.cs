using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NosAyudamos
{
    [AttributeUsage(AttributeTargets.Class)]
    class TableAttribute : Attribute
    {
        static readonly Regex validator = new Regex("^[A-Za-z][A-Za-z0-9]{2,62}$", RegexOptions.Compiled);

        public TableAttribute(string name)
        {
            if (!validator.IsMatch(name))
                throw new ArgumentException($"Table name '{name}' contains invalid characters.", nameof(name));

            Name = name;
        }

        public string Name { get; }
    }

    /// <summary>
    /// Flags the property to use as the table storage row key 
    /// when storing the annotated type using the <see cref="EntityRepository{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class RowKeyAttribute : TableStorageAttribute
    {
        /// <summary>
        /// Creates a strong-typed fast accessor for the property annotated 
        /// with <see cref="RowKeyAttribute"/> for instances of the given type 
        /// <typeparamref name="TEntity"/>.
        /// </summary>
        public static Func<TEntity, string> CreateAccessor<TEntity>() => CreateAccessor<TEntity, RowKeyAttribute>();
    }

    /// <summary>
    /// Flags the property to use as the table storage partition key 
    /// when storing the annotated type using the <see cref="EntityRepository{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class PartitionKeyAttribute : TableStorageAttribute
    {
        /// <summary>
        /// Creates a strong-typed fast accessor for the property annotated 
        /// with <see cref="PartitionKeyAttribute"/> for instances of the given type 
        /// <typeparamref name="TEntity"/>.
        /// </summary>
        public static Func<TEntity, string> CreateAccessor<TEntity>() => CreateAccessor<TEntity, PartitionKeyAttribute>();
    }

    abstract class TableStorageAttribute : Attribute
    {
        // See https://stackoverflow.com/questions/11514707/azure-table-storage-rowkey-restricted-character-patterns
        static readonly HashSet<char> InvalidChars = new HashSet<char>(new[]
        {
            ' ', '/', '\\', '#', '?', '\t', '\n', '\r', '+', '|', '[', ']', '{', '}', '<', '>', '$', '^', '&'
        });

        protected static Func<TEntity, string> CreateAccessor<TEntity, TAttribute>() where TAttribute : Attribute
        {
            var attributeName = typeof(TAttribute).Name.Substring(0, typeof(TAttribute).Name.Length - 9);

            var keyProp = typeof(TEntity).GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<TAttribute>() != null)
                ?? throw new ArgumentException($"Expected entity type '{typeof(TEntity).Name}' to have one property annotated with [{attributeName}]");

            if (keyProp.PropertyType != typeof(string))
                throw new ArgumentException($"Property '{typeof(TEntity).Name}.{keyProp.Name}' annotated with [{attributeName}] must be of type string.");

            var param = Expression.Parameter(typeof(TEntity), "entity");

            return Expression.Lambda<Func<TEntity, string>>(
                Expression.Block(
                    Expression.IfThen(
                        Expression.Equal(param, Expression.Constant(null)),
                        Expression.Throw(
                            Expression.New(
                                typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) }),
                                Expression.Constant("entity")))),
                    Expression.Call(
                        typeof(TableStorageAttribute).GetMethod(nameof(EnsureValid), BindingFlags.NonPublic | BindingFlags.Static),
                        Expression.Constant(attributeName),
                        Expression.Constant(keyProp.Name, typeof(string)),
                        Expression.Property(param, keyProp))),
                param)
               .Compile();
        }

        static string EnsureValid(string attributeName, string propertyName, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), $"[{attributeName}]-annotated property '{propertyName}' cannot be null.");

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"[{attributeName}]-annotated property '{propertyName}' cannot be empty.", nameof(value));

            if (value.Any(c => char.IsControl(c) || InvalidChars.Contains(c)))
                throw new ArgumentException($"Property '{propertyName}' has value '{value}', which contains invalid characters for [{attributeName}].", nameof(value));

            return value;
        }

        /// <summary>
        /// Sanitizes the value so that it can be used as a key in table storage (either PartitionKey or RowKey).
        /// </summary>
        public static string Sanitize(string value) => new string(value.Where(c => !char.IsControl(c) && !InvalidChars.Contains(c)).ToArray());
    }
}
