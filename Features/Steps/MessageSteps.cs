using Autofac;
using NosAyudamos.Events;
using TechTalk.SpecFlow;
using Xunit;
using System;
using System.Threading.Tasks;

namespace NosAyudamos.Steps
{
    [Binding]
    public class MessageSteps
    {
        ScenarioContext context;
        IEventStreamAsync events;
        MessageSent sent;

        public MessageSteps(FeatureContainer container, ScenarioContext context)
        {
            this.context = context;
            events = container.Resolve<IEventStreamAsync>();
            events
                .Of<MessageSent>()
                .Subscribe(e => sent = e);
        }

        [When(@"Envia (.*)")]
        [When(@"Envia mensaje")]
        public async Task WhenMessageReceived(string message)
        {
            if (context.TryGetValue<Person>(out var person))
            {
                await events.PushAsync(new TextMessageReceived(person.PhoneNumber, Constants.System.PhoneNumber, message.ToSingleLine()));
            }
            else
            {
                await events.PushAsync(new TextMessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, message.ToSingleLine()));
            }
        }

        [Then(@"Recibe mensaje")]
        public void ThenMessageSent(string message)
        {
            Assert.NotNull(sent);
            Assert.Equal(message.ToSingleLine(), sent.Body);
        }
    }
}
