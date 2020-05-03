using System;

using Streamstone;
using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace NosAyudamos
{
    public class EventStoreTests
    {
        static CloudTable table;

        static EventStoreTests()
        {
            table = CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudTableClient()
                .GetTableReference("Tests");

            table.DeleteIfExistsAsync().Wait();
            table.CreateIfNotExistsAsync().Wait();
        }

        public async Task Persist()
        {
            var partition = new Partition(table, "23696294");

            var existent = await Stream.TryOpenAsync(partition);
            var stream = existent.Found ? existent.Stream : new Stream(partition);

            var person = new SourcedPerson("23696294", "Daniel", "Cazzulino");

            var entity = new PersonEntity(person.NationalId, person.FirstName, person.LastName, "", "M", "5491159278282", role: nameof(Role.Donor))
            {
                Version = stream.Version + person.Events.Count(),
            };
            //{
            //    FirstName = person.FirstName,
            //    LastName = person.LastName,
            //    DonatedAmount = person.DonatedAmount,
            //    // TODO: phone number
            //    Version = stream.Version + person.Events.Count(),
            //};

            await Stream.WriteAsync(stream, person.Events.Select((e, i) => ToEventData(e, entity, stream.Version + i)).ToArray());

            stream = await Stream.OpenAsync(partition);

            var version = person.Events.Count();

            person.AcceptEvents();

            person.Donate(500);
            person.Donate(1000);

            entity = new PersonEntity(person.NationalId, person.FirstName, person.LastName, "", "M", "5491159278282", role: nameof(Role.Donor))
            {
                DonatedAmount = person.DonatedAmount,
                Version = stream.Version + person.Events.Count(),
            };

            await Stream.WriteAsync(stream, person.Events.Select((e, i) => ToEventData(e, entity, version + i)).ToArray());

            // collect all processed events for given aggregate and return them as a list
            // used to build up an aggregate from its history (Domain.LoadsFromHistory)
            var events = (await Stream.ReadAsync<EventEntity>(partition)).Events.Select(ToEvent).ToList();

            var loaded = new SourcedPerson(events);

            Assert.Equal(person.NationalId, loaded.NationalId);
            Assert.Equal(person.DonatedAmount, loaded.DonatedAmount);
        }

        static DomainEvent ToEvent(DomainEventEntity entity)
        {
            var e = (DomainEvent)JsonConvert.DeserializeObject(entity.Data, Type.GetType(entity.EventType));
            e.Version = entity.Version;
            e.When = entity.Timestamp;
            return e;
        }

        static EventData ToEventData(DomainEvent e, PersonEntity person, int version)
        {
            var id = Guid.NewGuid();
            var dataVersion = e.GetType().Assembly.GetName().Version;

            var properties = new
            {
                Id = id,
                EventType = e.GetType().FullName,
                Data = (string)new Serializer().Serialize(e),
                DataVersion = $"{dataVersion.Major}.{dataVersion.Minor}",
                Version = version,
            };

            return new EventData(
                EventId.From(id),
                EventProperties.From(properties),
                EventIncludes.From(Include.InsertOrReplace(person)));
        }
    }

    public class Registered : DomainEvent
    {
        public Registered(string nationalId, string firstName, string lastName)
            => (NationalId, FirstName, LastName) = (nationalId, firstName, lastName);

        public string NationalId { get; }
        public string FirstName { get; }
        public string LastName { get; }
    }

    public class Donated : DomainEvent
    {
        public Donated(double amount) => Amount = amount;

        public double Amount { get; }
    }
}
