using Autofac;
using TechTalk.SpecFlow;

namespace NosAyudamos.Steps
{
    [Binding]
    public class GivenSteps
    {
        IContainer container;
        ScenarioContext context;

        public GivenSteps(FeatureContainer container, ScenarioContext context)
            => (this.container, this.context) = (container, context);

        [Given(@"Un usuario no registrado")]
        public void GivenAnUnregisteredUser() { }

        [Given(@"Un donante que quiere ayudar")]
        public void DadoUnDonanteQueQuiereAyudar()
        {
            context.Pending();
        }

        [Given(@"Un donatario")]
        public void GivenADonee()
        {
            var repo = container.Resolve<IPersonRepository>();
            var person = Constants.Donee.Create();

            repo.PutAsync(person);
            context.Set(person);
        }

        [Given(@"Un donante")]
        public void GivenADonor()
        {
            var repo = container.Resolve<IPersonRepository>();
            var person = Constants.Donor.Create();

            repo.PutAsync(person);
            context.Set(person);
        }

        [Given(@"Una persona '(.*)' con DNI '(.*)' y telefono '(.*)'")]
        public void GivenAPerson(string fullName, string nationalId, string phoneNumber)
        {
            var repo = container.Resolve<IPersonRepository>();
            var names = fullName.Split(' ');

            var person = new Person(string.Join(' ', names[..^1]), names[^1], nationalId, phoneNumber);

            repo.PutAsync(person);

            context.Set(person);
        }
    }
}
