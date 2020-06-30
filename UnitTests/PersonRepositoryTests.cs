using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamstone;
using Xunit;

namespace NosAyudamos
{
    public class PersonRepositoryTests
    {
        static readonly ISerializer serializer = new Serializer();

        public PersonRepositoryTests()
        {
            CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudTableClient()
                .GetTableReference(nameof(Person)).DeleteIfExists();
        }

        [Fact]
        public void PersonSerialization()
        {
            var person = Constants.Donee.Create();
            var json = JsonConvert.SerializeObject(person);
            var obj = JObject.Parse(json);
            obj["TaxStatus"] = TaxStatus.Validated.ToString();

            var actual = JsonConvert.DeserializeObject<Donee>(obj.ToString());

            Assert.Equal(TaxStatus.Validated, actual.TaxStatus);
        }

        [Fact]
        public async Task ConcurrentSaveFails()
        {
            var repo = new PersonRepository(serializer, CloudStorageAccount.DevelopmentStorageAccount);

            await repo.PutAsync(Constants.Donor.Create());

            var first = await repo.GetAsync<Donor>(Constants.Donor.Id, readOnly: false);
            var second = await repo.GetAsync<Donor>(Constants.Donor.Id, readOnly: false);

            first.Donate(500);
            second.Donate(500);

            await repo.PutAsync(first);

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => repo.PutAsync(second));
        }

        [Fact]
        public async Task CanManipulatePersonAndHistory()
        {
            var repo = new PersonRepository(serializer, CloudStorageAccount.DevelopmentStorageAccount);

            await repo.PutAsync(Constants.Donor.Create());

            var expected = await repo.GetAsync<Donor>(Constants.Donor.Id);

            Assert.True(expected.IsReadOnly);
            Assert.Empty(expected.History);

            Assert.Throws<InvalidOperationException>(() => expected.Donate(500));

            var person = await repo.GetAsync<Donor>(Constants.Donor.Id, readOnly: false);

            Assert.False(person.IsReadOnly);

            Assert.Equal(expected.PersonId, person.PersonId);
            Assert.Equal(expected.FirstName, person.FirstName);
            Assert.Equal(expected.LastName, person.LastName);
            Assert.Equal(expected.PhoneNumber, person.PhoneNumber);
            Assert.Equal(expected.DateOfBirth, person.DateOfBirth);
            Assert.Equal(expected.Sex, person.Sex);

            person.Donate(500);

            Assert.Single(person.Events);

            person = await repo.PutAsync(person);

            Assert.Empty(person.Events);

            person = await repo.GetAsync<Donor>(Constants.Donor.Id, readOnly: false);

            Assert.NotEmpty(person.History);
            Assert.Equal(500, person.TotalDonated);

            person.Donate(1000);

            Assert.Equal(1500, person.TotalDonated);
            Assert.Single(person.Events);

            await repo.PutAsync(person);
            person = await repo.GetAsync<Donor>(Constants.Donor.Id, readOnly: true);

            // History is not loaded when creating readonly
            Assert.Empty(person.History);
            Assert.Equal(1500, person.TotalDonated);

            person = await repo.GetAsync<Donor>(Constants.Donor.Id, readOnly: false);

            Assert.All(person.History, h => Assert.Equal(Constants.Donor.Id, h.SourceId));

            person.UpdatePhoneNumber(Constants.Donee.PhoneNumber);

            Assert.Single(person.Events);

            await repo.PutAsync(person);

            person = await repo.GetAsync<Donor>(Constants.Donor.Id, readOnly: true);

            Assert.Equal(Constants.Donee.PhoneNumber, person.PhoneNumber);
        }
    }
}
