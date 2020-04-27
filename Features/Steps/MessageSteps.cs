using Autofac;
using Merq;
using NosAyudamos.Events;
using TechTalk.SpecFlow;
using Xunit;
using System;

namespace NosAyudamos.Steps
{
    [Binding]
    public class MessageSteps
    {
        ScenarioContext context;
        IEventStream events;
        MessageSent sent;

        public MessageSteps(FeatureContainer container, ScenarioContext context)
        {
            this.context = context;
            events = container.Resolve<IEventStream>();
            events
                .Of<MessageSent>()
                .Subscribe(e => sent = e);
        }

        [When(@"Envia (.*)")]
        [When(@"Envia mensaje")]
        public void WhenMessageReceived(string message)
        {
            if (context.TryGetValue<Person>(out var person))
            {
                events.Push(new MessageReceived(person.PhoneNumber, Constants.System.PhoneNumber, message.ToSingleLine()));
            }
            else
            {
                events.Push(new MessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, message.ToSingleLine()));
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
