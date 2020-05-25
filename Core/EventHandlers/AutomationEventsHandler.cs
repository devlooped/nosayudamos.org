using System.Net.Http;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class AutomationEventsHandler : IEventHandler<AutomationPaused>, IEventHandler<AutomationResumed>
    {
        readonly IEntityRepository<PhoneSystem> repository;
        readonly HttpClient http;

        public AutomationEventsHandler(IEntityRepository<PhoneSystem> repository, HttpClient http)
            => (this.repository, this.http)
            = (repository, http);

        public async Task HandleAsync(AutomationResumed e)
        {
            var phone = await repository.GetAsync(e.PhoneNumber);
            if (phone != null && phone.AutomationPaused == true)
            {
                phone.AutomationPaused = false;
                await repository.PutAsync(phone);
            }
        }

        public async Task HandleAsync(AutomationPaused e)
        {
            var phone = await repository.GetAsync(e.PhoneNumber);
            if (phone == null)
                phone = new PhoneSystem(e.PhoneNumber, "");

            if (phone.AutomationPaused != true)
            {
                phone.AutomationPaused = true;
                await repository.PutAsync(phone);
            }
        }
    }
}
