﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Xunit;
using Xunit.Abstractions;

namespace NosAyudamos
{
    public class RequestTests
    {
        const string tableName = nameof(RequestTests);
        readonly ITestOutputHelper output;

        public RequestTests(ITestOutputHelper output)
        {
            this.output = output;

            CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudTableClient()
                .GetTableReference(tableName).DeleteIfExists();
        }

        [Fact]
        public void WhenSerializingRequestCreated_ThenCanDeserialize()
        {
            var serializer = new Serializer();

            var expected = new RequestCreated(Constants.Donee.Id + "-" + Base62.Encode(PreciseTime.UtcNow.Ticks), 0, "");

            var json = serializer.Serialize(expected);

#if DEBUG
            output.WriteLine(json);
#endif

            var actual = serializer.Deserialize<RequestCreated>(json);

            Assert.Equal(expected.PersonId, actual.PersonId);
            Assert.Equal(expected.RequestId, actual.RequestId);
        }

        [Fact]
        public void WhenDoneeRequestsHelp_ThenRequestIsCreated()
        {
            var person = Constants.Donee.Create();
            person.ApproveTaxStatus("kzu");
            person.AcceptEvents();

            var request = person.Request(1000, "Necesito 1000 para supermercado", Array.Empty<string>());

            Assert.Single(person.Events);

            var requested = person.Events.Cast<Requested>().First();

            Assert.Equal(1000, requested.Amount);

            Assert.Equal(requested.Amount, request.Amount);
            Assert.Equal(requested.Description, request.Description);
            Assert.Equal(requested.Keywords, request.Keywords);

            Assert.Equal(1000, person.TotalRequested);

            Assert.Single(request.Events);

            var created = request.Events.Cast<RequestCreated>().First();

            Assert.Equal(requested.Amount, created.Amount);
            Assert.Equal(requested.Description, created.Description);
            Assert.Equal(requested.Keywords, created.Keywords);
        }

        [Fact]
        public async Task WhenReplying_AddsReplyToRequest()
        {
            var requests = new RequestRepository(new Serializer(), CloudStorageAccount.DevelopmentStorageAccount, tableName);

            var request = new Request(Constants.Donee.Id, 1000, "Necesito 1000 para supermercado");

            await requests.PutAsync(request);

            request = await requests.GetAsync(request.RequestId, false);

            request.Reply(Constants.Donor.Id, "What are you buying?");
            request.Reply(Constants.Donee.Id, "Food");

            Assert.Equal(2, request.Events.Count());
            Assert.Equal(Constants.Donor.Id, request.Events.Cast<RequestReplied>().First().SenderId);
            Assert.Equal(Constants.Donee.Id, request.Events.Cast<RequestReplied>().Skip(1).First().SenderId);

            await requests.PutAsync(request);

            Assert.Empty(request.Events);

            var actual = await requests.GetAsync(request.RequestId);

            Assert.Equal(request.RequestId, actual.RequestId);
            Assert.Equal(request.PersonId, actual.PersonId);
            Assert.Equal(request.Description, actual.Description);
            Assert.Equal(request.Messages, actual.Messages);
        }
    }
}
