using System.Linq;
using System.Threading.Tasks;

namespace NosAyudamos.EventHandlers
{
    class DoneeWelcomeHandler : IEventHandler<TaxStatusAccepted>, IEventHandler<TaxStatusApproved>
    {
        readonly IPersonRepository peopleRepo;
        readonly IEntityRepository<PhoneSystem> phoneDir;
        readonly IEventStreamAsync events;

        public DoneeWelcomeHandler(IPersonRepository peopleRepo, IEntityRepository<PhoneSystem> phoneDir, IEventStreamAsync events)
            => (this.peopleRepo, this.phoneDir, this.events)
            = (peopleRepo, phoneDir, events);

        public async Task HandleAsync(TaxStatusAccepted e)
        {
            var person = await peopleRepo.GetAsync(e.SourceId!).ConfigureAwait(false);
            if (person == null)
                return;

            await WelcomeAsync(person);
        }

        public async Task HandleAsync(TaxStatusApproved e)
        {
            var person = await peopleRepo.GetAsync(e.SourceId!).ConfigureAwait(false);
            if (person == null)
                return;

            if (person.Role == Role.Donee)
                await WelcomeAsync(person);
        }

        async Task WelcomeAsync(Person donee)
        {
            await events.PushAsync(new MessageSent(
                donee.PhoneNumber,
                Strings.UI.Donee.Welcome(donee.FirstName.Split(' ').First(), donee.Sex == Sex.Male ? "o" : "a")))
                .ConfigureAwait(false);

            await events.PushAsync(new MessageSent(donee.PhoneNumber, Strings.UI.Donee.Instructions))
                .ConfigureAwait(false);

            var phone = await phoneDir.GetAsync(donee.PhoneNumber).ConfigureAwait(false);

            // If automation was paused for some reason, it's time to resume it
            if (phone?.AutomationPaused == true)
            {
                phone.AutomationPaused = false;
                await phoneDir.PutAsync(phone).ConfigureAwait(false);
            }
        }
    }
}
