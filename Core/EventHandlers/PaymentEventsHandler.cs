using System.Net.Http;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class PaymentEventsHandler :
        IEventHandler<PaymentCodeReceived>,
        IEventHandler<PaymentRequested>,
        IEventHandler<PaymentApproved>
    {
        readonly IEnvironment env;
        readonly IPersonRepository peopleRepo;
        readonly HttpClient http;

        public PaymentEventsHandler(IEnvironment env, IPersonRepository peopleRepo, HttpClient http)
            => (this.env, this.peopleRepo, this.http)
            = (env, peopleRepo, http);

        public Task HandleAsync(PaymentCodeReceived e) => Task.CompletedTask;

        public Task HandleAsync(PaymentRequested e) => Task.CompletedTask;

        public Task HandleAsync(PaymentApproved e) => Task.CompletedTask;
    }
}
