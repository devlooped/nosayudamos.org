using System;
using Xunit;

namespace NosAyudamos
{
    public class AttributesTests
    {
        [Fact]
        public void WhenGettingPartitionKeyWithoutAnnotatedProperty_ThenThrows()
            => Assert.Throws<ArgumentException>(() => PartitionKeyAttribute.CreateAccessor<EntityWithoutAttributes>());

        [Fact]
        public void WhenGettingRowKeyWithoutAnnotatedProperty_ThenThrows()
            => Assert.Throws<ArgumentException>(() => RowKeyAttribute.CreateAccessor<EntityWithoutAttributes>());

        [Fact]
        public void WhenGettingNonStringPartitionKey_ThenThrows()
            => Assert.Throws<ArgumentException>(() => PartitionKeyAttribute.CreateAccessor<EntityWithAttributesWrongTypes>());

        [Fact]
        public void WhenGettingNonStringRowKey_ThenThrows()
            => Assert.Throws<ArgumentException>(() => RowKeyAttribute.CreateAccessor<EntityWithAttributesWrongTypes>());

        [Fact]
        public void WhenGettingPartitionKeyNullEntity_ThenThrows()
            => Assert.Throws<ArgumentNullException>(() => PartitionKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(null));

        [Fact]
        public void WhenGettingRowKeyNullEntity_ThenThrows()
            => Assert.Throws<ArgumentNullException>(() => RowKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(null));

        [Fact]
        public void WhenGettingPartitionKeyNullPropertyValue_ThenThrows()
            => Assert.Throws<ArgumentNullException>(() => PartitionKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes()));

        [Fact]
        public void WhenGettingRowKeyNullPropertyValue_ThenThrows()
            => Assert.Throws<ArgumentNullException>(() => RowKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes()));

        [Fact]
        public void WhenGettingPartitionKeyEmptyPropertyValue_ThenThrows()
            => Assert.Throws<ArgumentException>(() => PartitionKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes { PartitionKey = "" }));

        [Fact]
        public void WhenGettingRowKeyEmptyPropertyValue_ThenThrows()
            => Assert.Throws<ArgumentException>(() => RowKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes { RowKey = "" }));

        [Fact]
        public void WhenGettingPartitionKeyWhitespacePropertyValue_ThenThrows()
            => Assert.Throws<ArgumentException>(() => PartitionKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes { PartitionKey = "  " }));

        [Fact]
        public void WhenGettingRowKeyWhitespacePropertyValue_ThenThrows()
            => Assert.Throws<ArgumentException>(() => RowKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes { RowKey = "" }));

        [Fact]
        public void WhenGettingPartitionKeyWithInvalidChar_ThenThrows()
            => Assert.Throws<ArgumentException>(() => PartitionKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes { PartitionKey = " \\ " }));

        [Fact]
        public void WhenGettingRowKeyWithInvalidChar_ThenThrows()
            => Assert.Throws<ArgumentException>(() => RowKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes { RowKey = "$\r\n" }));

        [Fact]
        public void WhenGettingPartitionKey_ThenReturnsValue()
            => Assert.Equal("123", PartitionKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes { PartitionKey = "123" }));

        [Fact]
        public void WhenGettingRowKey_ThenReturnsValue()
            => Assert.Equal("asdf", RowKeyAttribute.CreateAccessor<EntityWithAttributes>().Invoke(new EntityWithAttributes { RowKey = "asdf" }));

        public class EntityWithoutAttributes
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
        }

        public class EntityWithAttributesWrongTypes
        {
            [PartitionKey]
            public int PartitionKey { get; set; }
            [RowKey]
            public int RowKey { get; set; }
        }

        public class EntityWithAttributes
        {
            [PartitionKey]
            public string PartitionKey { get; set; }
            [RowKey]
            public string RowKey { get; set; }
        }
    }
}
