using System.Linq;
using System.Threading.Tasks;

namespace NosAyudamos.EventHandlers
{
    class TaxStatusRejectedHandler : IEventHandler<TaxStatusRejected>
    {
        readonly IPersonRepository peopleRepo;
        readonly IEntityRepository<PhoneSystem> phoneDir;
        readonly IEventStreamAsync events;

        public TaxStatusRejectedHandler(IPersonRepository peopleRepo, IEntityRepository<PhoneSystem> phoneDir, IEventStreamAsync events)
            => (this.peopleRepo, this.phoneDir, this.events)
            = (peopleRepo, phoneDir, events);

        public async Task HandleAsync(TaxStatusRejected e)
        {
            var person = await peopleRepo.GetAsync<Donee>(e.SourceId!).ConfigureAwait(false);
            if (person == null)
                return;

            var phone = await phoneDir.GetAsync(person.PhoneNumber).ConfigureAwait(false);
            if (phone == null)
                return;

            // Disable automation for this user from now on until resumed manually.
            phone.AutomationPaused = true;
            await phoneDir.PutAsync(phone).ConfigureAwait(false);

            // Send rejection notification message.
            var name = person.FirstName.Split(' ').First();
            var body = e.Reason switch
            {
                TaxStatusRejectedReason.NotApplicable => Strings.UI.Donee.NotApplicable(name),
                TaxStatusRejectedReason.HasIncomeTax => Strings.UI.Donee.HasIncomeTax(name),
                TaxStatusRejectedReason.HighCategory => Strings.UI.Donee.HighCategory(name),
                _ => Strings.UI.Donee.Rejected,
            };

            await events.PushAsync(new MessageSent(person.PhoneNumber, body)).ConfigureAwait(false);
        }
    }
}
