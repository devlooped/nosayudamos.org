using System.Net.Http;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class AutomationEventsHandler : IEventHandler<AutomationPaused>, IEventHandler<AutomationResumed>
    {
        readonly IEntityRepository<PhoneSystem> phoneDir;
        readonly HttpClient http;

        public AutomationEventsHandler(IEntityRepository<PhoneSystem> phoneDir, HttpClient http)
            => (this.phoneDir, this.http)
            = (phoneDir, http);

        public async Task HandleAsync(AutomationResumed e)
        {
            var phone = await phoneDir.GetAsync(e.PhoneNumber);
            if (phone != null && phone.AutomationPaused == true)
            {
                phone.AutomationPaused = false;
                await phoneDir.PutAsync(phone);
            }
        }

        public async Task HandleAsync(AutomationPaused e)
        {
            var phone = await phoneDir.GetAsync(e.PhoneNumber);
            if (phone == null)
                phone = new PhoneSystem(e.PhoneNumber, "");

            if (phone.AutomationPaused != true)
            {
                phone.AutomationPaused = true;
                await phoneDir.PutAsync(phone);
            }
        }
    }
}
