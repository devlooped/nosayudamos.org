using System.Net.Http;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class DonationEventsHandler : IEventHandler<DonationReceived>, IEventHandler<SubscriptionReceived>
    {
        readonly IEnvironment env;
        readonly HttpClient http;
        readonly IPersonRepository people;

        public DonationEventsHandler(IEnvironment env, HttpClient http, IPersonRepository people)
            => (this.env, this.http, this.people)
            = (env, http, people);

        public Task HandleAsync(SubscriptionReceived e) => Task.CompletedTask;

        public Task HandleAsync(DonationReceived e) => Task.CompletedTask;
    }
}
