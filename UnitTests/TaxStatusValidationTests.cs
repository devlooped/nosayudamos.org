using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace NosAyudamos
{
    public class TaxStatusValidationTests
    {
        [Fact]
        public async Task WhenHandlingDoneeRegistered_ThenCreatesValidation()
        {
            var donee = Constants.Donee.Create();
            var repo = new TestEntityRepository<TaxStatusValidation>();
            var handler = new TaxStatusValidationHandler(
                Mock.Of<ITaxIdRecognizer>(),
                Mock.Of<IPersonRepository>(x =>
                    x.GetAsync<Donee>(donee.PersonId, false) == Task.FromResult(donee)),
                repo);

            await handler.HandleAsync(Constants.Donee.Create().Events.OfType<PersonRegistered>().First());

            Assert.NotNull(await repo.GetAsync(Constants.Donee.Id));
        }

        [Fact]
        public async Task WhenDoneeNotFound_ThenDeletesValidation()
        {
            var repo = new Mock<IEntityRepository<TaxStatusValidation>>();

            var handler = new TaxStatusValidationHandler(
                Mock.Of<ITaxIdRecognizer>(),
                Mock.Of<IPersonRepository>(),
                repo.Object);

            var validation = new TaxStatusValidation(Constants.Donee.Id);

            await handler.ExecuteAsync(validation);

            repo.Verify(x => x.DeleteAsync(validation));
        }

        [Fact]
        public async Task WhenDoneeValidated_ThenDeletesValidationAndStops()
        {
            var repo = new Mock<IEntityRepository<TaxStatusValidation>>();
            var donee = Constants.Donee.Create();
            donee.UpdateTaxStatus(TaxId.None);

            var handler = new TaxStatusValidationHandler(
                new Mock<ITaxIdRecognizer>(MockBehavior.Strict).Object,
                Mock.Of<IPersonRepository>(x =>
                    x.GetAsync<Donee>(donee.PersonId, false) == Task.FromResult(donee)),
                repo.Object);

            var validation = new TaxStatusValidation(Constants.Donee.Id);

            await handler.ExecuteAsync(validation);

            repo.Verify(x => x.DeleteAsync(validation));
        }

        [Fact]
        public async Task WhenDoneeRejected_ThenDeletesValidationAndStops()
        {
            var repo = new Mock<IEntityRepository<TaxStatusValidation>>();
            var donee = Constants.Donee.Create();
            donee.UpdateTaxStatus(new TaxId(donee.PersonId) { HasIncomeTax = true });

            Assert.Equal(TaxStatus.Rejected, donee.TaxStatus);

            var handler = new TaxStatusValidationHandler(
                new Mock<ITaxIdRecognizer>(MockBehavior.Strict).Object,
                Mock.Of<IPersonRepository>(x =>
                    x.GetAsync<Donee>(donee.PersonId, false) == Task.FromResult(donee)),
                repo.Object);

            var validation = new TaxStatusValidation(Constants.Donee.Id);

            await handler.ExecuteAsync(validation);

            repo.Verify(x => x.DeleteAsync(validation));
        }

        [Fact]
        public async Task WhenDoneePending_ThenValidates()
        {
            var repo = new Mock<IEntityRepository<TaxStatusValidation>>();
            var people = new Mock<IPersonRepository>();
            var donee = Constants.Donee.Create();

            Assert.Equal(TaxStatus.Unknown, donee.TaxStatus);

            people.Setup(x => x.GetAsync<Donee>(donee.PersonId, false))
                .ReturnsAsync(donee);

            var handler = new TaxStatusValidationHandler(
                Mock.Of<ITaxIdRecognizer>(x =>
                    x.RecognizeAsync(donee) == Task.FromResult(TaxId.None)),
                people.Object,
                repo.Object);

            var validation = new TaxStatusValidation(Constants.Donee.Id);

            await handler.ExecuteAsync(validation);

            Assert.Equal(TaxStatus.Validated, donee.TaxStatus);

            people.Verify(x => x.PutAsync(donee));
            repo.Verify(x => x.DeleteAsync(validation));
        }

        [Fact]
        public async Task WhenHandlingDonorRegistered_ThenNoOp()
        {
            var repo = new TestEntityRepository<TaxStatusValidation>();
            var handler = new TaxStatusValidationHandler(
                Mock.Of<ITaxIdRecognizer>(),
                new Mock<IPersonRepository>(MockBehavior.Strict).Object,
                repo);

            await handler.HandleAsync(Constants.Donor.Create().Events.OfType<PersonRegistered>().First());
        }
    }
}
