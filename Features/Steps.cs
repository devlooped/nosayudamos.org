using Autofac;
using TechTalk.SpecFlow;
using Xunit;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace NosAyudamos.Steps
{
    [Binding]
    public class Steps
    {
        IContainer container;
        ScenarioContext context;
        IEventStreamAsync events;
        MessageSent sent;

        public Steps(FeatureContainer container, ScenarioContext context)
        {
            Skip.If(context.ScenarioInfo.Tags.Contains("Draft"));

            this.container = container;
            this.context = context;

            events = container.Resolve<IEventStreamAsync>();
            events
                .Of<MessageSent>()
                .Subscribe(e => sent = e);
        }

        #region Given

        [Given(@"(.*)\s?=\s?(.*)")]
        public void GivenEnvironmentVariable(string name, string value)
            => container.Resolve<FeatureEnvironment>().SetVariable(name, value);

        [Given(@"Un storage limpio")]
        public async Task GivenAClearStorage()
            => await CloudStorageAccount.DevelopmentStorageAccount.ClearStorageAsync();

        [Given(@"Un usuario no registrado")]
        public void GivenAnUnregisteredUser() { }

        [Given(@"Un donador(.*)")]
        [Given(@"un donante(.*)")]
        public async Task GivenADonor(string _ = null) =>
            context.Set(await container.Resolve<IPersonRepository>().PutAsync(Constants.Donor.Create()));

        [Given(@"Un donatario")]
        public async Task GivenADonee() =>
            context.Set(await container.Resolve<IPersonRepository>().PutAsync(Constants.Donee.Create()));

        [Given(@"Una persona '(.*)' con DNI '(.*)' y telefono '(.*)'")]
        public void GivenAPerson(string fullName, string nationalId, string phoneNumber)
        {
            var repo = container.Resolve<IPersonRepository>();
            var names = fullName.Split(' ');

            var person = new Person(string.Join(' ', names[..^1]), names[^1], nationalId, phoneNumber);

            repo.PutAsync(person);

            context.Set(person);
        }

        #endregion

        #region When

        [When(@"Envia ID (.*)")]
        public async Task WhenReceivedId(string id)
        {
            var file = id.Trim('\'').Trim('"');
            Skip.IfNot(File.Exists(file), $"File '{id}' not found.");
            await events.PushAsync(new MessageReceived(
                Constants.Donee.PhoneNumber,
                Constants.System.PhoneNumber,
                new Uri(new FileInfo(file).FullName).ToString()));
        }

        [When(@"Envia '(.*)'")]
        [When(@"Envia ""(.*)""")]
        [When(@"Envia (?!ID).*")]
        [When(@"Envia mensaje")]
        public async Task WhenMessageReceived(string message)
        {
            if (context.TryGetValue<Person>(out var person))
            {
                await events.PushAsync(new MessageReceived(person.PhoneNumber, Constants.System.PhoneNumber, message.ToSingleLine()));
            }
            else
            {
                await events.PushAsync(new MessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, message.ToSingleLine()));
            }
        }

        #endregion

        #region Then

        [Then(@"Recibe '(.*)'")]
        [Then(@"Recibe ""(.*)""")]
        [Then(@"Recibe ([A-Z_]+)(\s.*)?")]
        public void ThenMessageSent(string resource, string message = "")
        {
            var value = Resources.ResourceManager.GetString(resource);

            Assert.True(value.Equals(message.ToSingleLine()), $"Resource {resource} does not match the given message.");
            Assert.True(sent != null, "No message was sent.");

            var format = Regex.Replace(value, "{\\d}", ".+");
            var matches = Regex.IsMatch(sent.Body, format);

            if (value.Contains('{'))
            {
                Assert.True(matches, @$"Sent message:
{sent.Body}
does not match:
{format}");
            }
            else
            {
                Assert.Equal(message.ToSingleLine(), sent.Body);
            }
        }

        #endregion
    }
}
