using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace NosAyudamos
{
    public class EntityRepositoryTests
    {
        public async Task CanSavePersonMessagingMap()
        {
            var expected = new PersonMessagingMap("23696294", "123", "987");

            var repository = new EntityRepository<PersonMessagingMap>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer());

            await repository.PutAsync(expected);

            var actual = await repository.GetAsync(expected.PersonId);

            Assert.Equal(expected.PersonId, actual.PersonId);
            Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
            Assert.Equal(expected.SystemNumber, actual.SystemNumber);
        }

        public async Task CanSaveNested()
        {
            var expected = new NestedType
            {
                Id = "123",
                IntProp = 25,
                StringProp = "Foo"
            };

            var repository = new EntityRepository<NestedType>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer());

            await repository.PutAsync(expected);

            var actual = await repository.GetAsync(expected.Id);

            Assert.Equal(expected.StringProp, actual.StringProp);

        }

        class NestedType
        {
            [Key]
            public string Id { get; set; }
            public int IntProp { get; set; }
            public string StringProp { get; set; }
        }
    }
}
