using System.Linq;
using System.Threading.Tasks;

namespace NosAyudamos
{
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
