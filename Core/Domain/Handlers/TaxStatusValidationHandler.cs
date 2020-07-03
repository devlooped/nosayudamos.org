using System.Threading.Tasks;

namespace NosAyudamos
{
    /// <summary>
    /// Represents a pending tax status validation for a newly registered 
    /// donee.
    /// </summary>
    public class TaxStatusValidation
    {
        public TaxStatusValidation(string personId) => PersonId = personId;

        /// <summary>
        /// The identifier of the person requiring tax validation.
        /// </summary>
        [RowKey]
        public string PersonId { get; }
    }

    class TaxStatusValidationHandler : IEventHandler<PersonRegistered>
    {
        readonly ITaxIdRecognizer recognizer;
        readonly IPersonRepository personRepo;
        readonly IEntityRepository<TaxStatusValidation> entityRepo;

        public TaxStatusValidationHandler(
            ITaxIdRecognizer recognizer, IPersonRepository personRepo, IEntityRepository<TaxStatusValidation> entityRepo)
            => (this.recognizer, this.personRepo, this.entityRepo)
            = (recognizer, personRepo, entityRepo);

        public async Task ExecuteAsync(TaxStatusValidation validation)
        {
            var person = await personRepo.GetAsync<Donee>(validation.PersonId, false);
            if (person == null)
            {
                await entityRepo.DeleteAsync(validation);
                return;
            }

            // It may have gotten validated already as part of the startup workflow 
            // quick-lookup.
            if (person.TaxStatus == TaxStatus.Rejected ||
                person.TaxStatus == TaxStatus.Validated)
            {
                await entityRepo.DeleteAsync(validation);
                return;
            }

            var tax = await recognizer.RecognizeAsync(person);
            if (tax != null)
            {
                person.UpdateTaxStatus(tax);
                await personRepo.PutAsync(person);
                await entityRepo.DeleteAsync(validation);
            }
        }

        public async Task HandleAsync(PersonRegistered e)
        {
            if (e.Role == Role.Donee)
            {
                var validation = new TaxStatusValidation(e.PersonId);
                // Persist the command so we can delete it when validation completes.
                await entityRepo.PutAsync(validation);
                // Attempt initial validation right-away, inline with the raised event.
                await ExecuteAsync(validation);
            }
        }
    }
}
