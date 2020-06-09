using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace NosAyudamos
{
    public class FundsTests
    {
        [Fact]
        public void WhenAddingFundsAddsAvailableAmount()
        {
            var funds = FundsManager.Create();

            funds.Add(amount: 500, from: Constants.Donor.Id);

            Assert.Equal(500, funds.AvailableAmount);

            funds.Add(amount: 500, from: Constants.Donor2.Id);

            Assert.Equal(1000, funds.AvailableAmount);

            Assert.Equal(2, funds.Events.OfType<FundsAdded>().Count());
        }

        [Fact]
        public void WhenSameDonorCanAddsFundsThenSums()
        {
            var funds = FundsManager.Create();

            funds.Add(amount: 500, from: Constants.Donor.Id);
            funds.Add(amount: 500, from: Constants.Donor.Id);

            Assert.Equal(1000, funds.AvailableAmount);

            Assert.Equal(2, funds.Events.OfType<FundsAdded>().Count());
        }

        [Fact]
        public void WhenRequestingFundsPlacesInQueue()
        {
            var funds = FundsManager.Create();

            funds.Request(amount: 1000, by: Constants.Donee.Id);
            funds.Request(amount: 1000, by: Constants.Donee2.Id);

            Assert.Equal(2000, funds.RequestedAmount);

            Assert.Equal(2, funds.Events.OfType<FundsRequested>().Count());
            Assert.Equal(2, funds.Requests.Count);
        }

        [Fact]
        public void WhenSameDoneeRequestsFundsThenPreviousRequestIsReplaced()
        {
            var funds = FundsManager.Create();

            funds.Request(amount: 500, by: Constants.Donee.Id);
            funds.Request(amount: 1000, by: Constants.Donee.Id);

            Assert.Equal(1000, funds.RequestedAmount);

            Assert.Single(funds.Requests);
        }

        [Fact]
        public void WhenSufficientBalanceExistsThenCanAssigningAllRequests()
        {
            var funds = FundsManager.Create();

            funds.Add(amount: 2000, from: Constants.Donor.Id);
            funds.Request(amount: 500, by: Constants.Donee.Id);
            funds.Request(amount: 1000, by: Constants.Donee2.Id);

            funds.Assign();

            Assert.Equal(500, funds.AvailableAmount);
            Assert.Equal(0, funds.RequestedAmount);
            Assert.Equal(1500, funds.AssignedAmount);
            Assert.Empty(funds.Requests);
        }

        [Fact]
        public void WhenSerializingThenEqualsProperties()
        {
            var funds = FundsManager.Create();

            funds.Add(amount: 2000, from: Constants.Donor.Id);
            funds.Request(amount: 500, by: Constants.Donee.Id);
            funds.Request(amount: 1000, by: Constants.Donee2.Id);

            var serializer = new Serializer();
            var json = serializer.Serialize(funds);
            var actual = serializer.Deserialize<FundsManager>(json);

            Assert.Equal(
                (funds.AssignedAmount, funds.AvailableAmount, funds.RequestedAmount),
                (actual.AssignedAmount, actual.AvailableAmount, actual.RequestedAmount));
        }

        [Fact]
        public async Task WhenPersistingThenSucceeds()
        {
            await CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudTableClient()
                .GetTableReference("Funds")
                .DeleteIfExistsAsync();

            var repo = new FundsManagerRepository(new Serializer(), CloudStorageAccount.DevelopmentStorageAccount);
            var funds = await repo.GetAsync(readOnly: false);

            funds.Add(amount: 2000, from: Constants.Donor.Id);
            funds.Request(amount: 500, by: Constants.Donee.Id);
            funds.Request(amount: 1000, by: Constants.Donee2.Id);

            funds.Assign();

            await repo.PutAsync(funds);

            var actual = await repo.GetAsync(readOnly: true);

            Assert.Equal(
                (funds.AssignedAmount, funds.AvailableAmount, funds.RequestedAmount),
                (actual.AssignedAmount, actual.AvailableAmount, actual.RequestedAmount));
        }

    }

}
