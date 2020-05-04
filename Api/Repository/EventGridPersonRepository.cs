using System.Linq;
using System.Threading.Tasks;

namespace NosAyudamos
{
    /// <summary>
    /// A decorator of <see cref="IPersonRepository"/> that pushes 
    /// events from changes in domain objects to EventGrid.
    /// </summary>
    // We make it non-discoverable since we register it as a decorator instead.
    [NoExport]
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

            foreach (var change in changes)
            {
                change.SourceId = person.Id;
                await events.PushAsync((dynamic)change);
            }

            return saved;
        }
    }
}
