﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
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

        //[Fact]
        public async Task SaveNewPerson()
        {
            var repo = new PersonRepository(serializer, CloudStorageAccount.DevelopmentStorageAccount);

            await repo.PutAsync(new Person("23696294", "Daniel", "Cazzulino", "5491159278282"));

            var expected = await repo.GetAsync("23696294");

            Assert.True(expected.IsReadOnly);

            Assert.Throws<InvalidOperationException>(() => expected.Donate(500));

            var person = await repo.GetAsync("23696294", false);

            Assert.False(person.IsReadOnly);

            Assert.Equal(expected.Id, person.Id);
            Assert.Equal(expected.FirstName, person.FirstName);
            Assert.Equal(expected.LastName, person.LastName);
            Assert.Equal(expected.PhoneNumber, person.PhoneNumber);
            Assert.Equal(expected.DateOfBirth, person.DateOfBirth);
            Assert.Equal(expected.Sex, person.Sex);

            person.Donate(500);

            Assert.Single(person.Events);

            person = await repo.PutAsync(person);

            Assert.Empty(person.Events);

            person = await repo.GetAsync("23696294", false);

            Assert.Equal(2, person.History.Count());
            Assert.Equal(500, person.DonatedAmount);

            person.Donate(1000);

            Assert.Equal(1500, person.DonatedAmount);
            Assert.Single(person.Events);

            await repo.PutAsync(person);
            person = await repo.GetAsync("23696294", true);

            // History is not loaded when creating readonly
            Assert.Equal(0, person.History.Count());
            Assert.Equal(1500, person.DonatedAmount);

            person = await repo.GetAsync("23696294", false);
            person.UpdatePhoneNumber("541156109999");

            Assert.Single(person.Events);

            await repo.PutAsync(person);

            person = await repo.GetAsync("23696294", true);

            Assert.Equal("541156109999", person.PhoneNumber);
        }
    }
}