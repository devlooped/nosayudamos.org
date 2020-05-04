using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace NosAyudamos
{
    /// <summary>
    /// A decorator of <see cref="IPersonRepository"/> that pushes 
    /// events from changes in domain objects to EventGrid.
    /// </summary>
    // We make it non-discoverable since we register it as a decorator instead.
    [PartNotDiscoverable]
    class EventGridPersonRepository : IPersonRepository
    {
        readonly IPersonRepository repository;
        readonly dynamic events;

        public EventGridPersonRepository(IPersonRepository repository, IEventStreamAsync events)
            => (this.repository, this.events)
            = (repository, events);

        public Task<Person?> FindAsync(string phoneNumber, bool readOnly = true)
            => repository.FindAsync(phoneNumber, readOnly);

        public Task<Person?> GetAsync(string id, bool readOnly = true)
            => repository.GetAsync(id, readOnly);

        public async Task<Person> PutAsync(Person person)
        {
            var changes = person.Events.ToArray();
            var saved = await repository.PutAsync(person);

            foreach (dynamic change in changes)
            {
                await events.PushAsync(change, new EventMetadata
                {
                    EventId = change.EventId.ToString(),
                    Subject = person.Id,
                });
            }

            return saved;
        }
    }
}
