using Autofac;
using Merq;
using NosAyudamos.Events;
using TechTalk.SpecFlow;
using Xunit;
using System;

namespace NosAyudamos.Steps
{
    [Binding]
    public class EventSteps
    {
        IEventStream events;
        MessageSent sent;

        public EventSteps(FeatureContainer container)
        {
            events = container.Resolve<IEventStream>();
            events
                .Of<MessageSent>()
                .Subscribe(e => sent = e);
        }

        [When(@"Envia mensaje")]
        public void WhenMessageReceived(string message)
            => events.Push(new MessageReceived(Constants.DoneeNumber, Constants.SystemNumber, message.ToSingleLine()));

        [Then(@"Recibe mensaje")]
        public void ThenMessageSent(string message)
        {
            Assert.NotNull(sent);
            Assert.Equal(message.ToSingleLine(), sent.Body);
        }
    }
}
