using System.Linq;
using System.Threading.Tasks;

namespace NosAyudamos
{
    /// <summary>
    /// A decorator of <see cref="IPersonRepository"/> that pushes 
    /// events from changes in domain objects to EventGrid.
    /// </summary>
    /// <devdoc>
    /// We make it non-discoverable since we register it as a decorator instead.
    /// </devdoc>
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

        public Task<TPerson?> GetAsync<TPerson>(string id, bool readOnly = true) where TPerson : Person
            => repository.GetAsync<TPerson>(id, readOnly);

        public async Task<TPerson> PutAsync<TPerson>(TPerson person) where TPerson : Person
        {
            var changes = person.Events.ToArray();
            var saved = await repository.PutAsync(person);

            foreach (var change in changes)
            {
                change.SourceId = person.PersonId;
                await events.PushAsync((dynamic)change);
            }

            return saved;
        }
    }

    /// <summary>
    /// A decorator of <see cref="IRequestRepository"/> that pushes 
    /// events from changes in domain objects to EventGrid.
    /// </summary>
    /// <devdoc>
    /// We make it non-discoverable since we register it as a decorator instead.
    /// </devdoc>
    [NoExport]
    class EventGridRequestRepository : IRequestRepository
    {
        readonly IRequestRepository repository;
        readonly dynamic events;

        public EventGridRequestRepository(IRequestRepository repository, IEventStreamAsync events)
            => (this.repository, this.events)
            = (repository, events);

        public Task<Request?> GetAsync(string requestId, bool readOnly = true)
            => repository.GetAsync(requestId, readOnly);

        public async Task<Request> PutAsync(Request request)
        {
            var changes = request.Events.ToArray();
            var saved = await repository.PutAsync(request);

            foreach (var change in changes)
            {
                change.SourceId = request.RequestId;
                await events.PushAsync((dynamic)change);
            }

            return saved;
        }
    }
}
