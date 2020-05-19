using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Merq;

namespace NosAyudamos
{
    public class ValidateTaxStatus : IAsyncCommand
    {
        public ValidateTaxStatus(string personId) => PersonId = personId;

        [Key]
        public string PersonId { get; }
    }

    class ValidateTaxStatusHandler : IAsyncCommandHandler<ValidateTaxStatus>, IEventHandler<PersonRegistered>
    {
        readonly IEventStreamAsync events;
        readonly ITaxIdRecognizer recognizer;
        readonly IPersonRepository personRepo;
        readonly IEntityRepository<ValidateTaxStatus> entityRepo;

        public ValidateTaxStatusHandler(
            IEventStreamAsync events, ITaxIdRecognizer recognizer,
            IPersonRepository personRepo, IEntityRepository<ValidateTaxStatus> entityRepo)
            => (this.events, this.recognizer, this.personRepo, this.entityRepo)
            = (events, recognizer, personRepo, entityRepo);

        public bool CanExecute(ValidateTaxStatus command) => true;

        public async Task ExecuteAsync(ValidateTaxStatus command, CancellationToken cancellation)
        {
            var person = await personRepo.GetAsync(command.PersonId, false);
            if (person == null)
            {
                await entityRepo.DeleteAsync(command);
                return;
            }

            // It may have gotten validated already as part of the startup workflow 
            // quick-lookup.
            if (person.TaxStatus == TaxStatus.Rejected ||
                person.TaxStatus == TaxStatus.Validated)
            {
                await entityRepo.DeleteAsync(command);
                return;
            }

            var tax = await recognizer.RecognizeAsync(person);
            if (tax != null)
            {
                person.UpdateTaxStatus(tax);
                await personRepo.PutAsync(person);
                await entityRepo.DeleteAsync(command);
            }
        }

        public async Task HandleAsync(PersonRegistered e)
        {
            if (e.Role == Role.Donee)
            {
                var command = new ValidateTaxStatus(e.Id);
                // Persist the command so we can delete it when validation completes.
                await entityRepo.PutAsync(command);
                // Attempt initial validation right-away, inline with the raised event.
                await ExecuteAsync(command, CancellationToken.None);
            }
        }
    }
}
