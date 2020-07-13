using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.VisualStudio.Threading;
using Moq;
using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using Slack.Webhooks.Interfaces;
using Xunit;

namespace NosAyudamos
{
    public class SlackSamples
    {
        TestEnvironment environment;
        PhoneEntry phoneEntry = new PhoneEntry(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber);

        public SlackSamples()
        {
            environment = new TestEnvironment();
            // Since these are samples run explicitly and manually, always send them to slack 
            // since that's the whole point of this class :)
            environment.SetVariable("SendToSlackInDevelopment", "true");
        }

        public async Task SendRejected()
        {
            var person = Constants.Donee.Create();
            await SendMessageAsync(new SlackMessage
            {
                Username = "nosayudamos",
                Blocks = new List<Block>
                {
                    new Divider(),
                    new Section
                    {
                        BlockId = "sender",
                        Fields = new List<TextObject>
                        {
                            new TextObject($":rejected: {person.PhoneNumber}") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/constancia/inscripcion/{person.PersonId}|CUIT sin Monotributo>")
                            {
                                Type = TextObject.TextType.Markdown
                            },
                        }
                    },
                },
            }, phoneEntry);

            await SendMessageAsync(new SlackMessage
            {
                Username = "nosayudamos",
                Blocks = new List<Block>
                {
                    new Divider(),
                    new Section
                    {
                        BlockId = "sender",
                        Fields = new List<TextObject>
                        {
                            new TextObject($":rejected: {person.PhoneNumber}") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/constancia/inscripcion/{person.PersonId}|CUIT paga ganancias>")
                            {
                                Type = TextObject.TextType.Markdown
                            },
                        }
                    },
                },
            }, phoneEntry);

            await SendMessageAsync(new SlackMessage
            {
                Username = "nosayudamos",
                Blocks = new List<Block>
                {
                    new Divider(),
                    new Section
                    {
                        BlockId = "sender",
                        Fields = new List<TextObject>
                        {
                            new TextObject($":rejected: {person.PhoneNumber}") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/constancia/inscripcion/{person.PersonId}|Monotributo categoría D>")
                            {
                                Type = TextObject.TextType.Markdown
                            },
                        }
                    },
                },
            }, phoneEntry);
        }

        public async Task SendApproved()
        {
            var person = Constants.Donee.Create();
            await SendMessageAsync(new SlackMessage
            {
                Username = "nosayudamos",
                Blocks = new List<Block>
                {
                    new Divider(),
                    new Section
                    {
                        BlockId = "sender",
                        Fields = new List<TextObject>
                        {
                            new TextObject($":approved: 5491159278282") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age})"),
                        }
                    },
                },
            }, phoneEntry);
        }

        public async Task SendUnknown()
        {
            await SendMessageAsync(new SlackMessage
            {
                Blocks = new List<Block>
                {
                    new Divider(),
                    new Section
                    {
                        BlockId = "sender",
                        Fields = new List<TextObject>
                        {
                            new TextObject($":unknown: {Constants.Donee.PhoneNumber}") { Emoji = true },
                            new TextObject($":whatsapp: {Constants.System.PhoneNumber}") { Emoji = true },
                        }
                    },
                    new Section
                    {
                        BlockId = "body",
                        Text = new TextObject($"> Gracias vieja!") { Type = TextObject.TextType.Markdown },
                    },
                    new Context
                    {
                        Elements = new List<IContextElement>
                        {
                            new TextObject($":help: 0.54 :donate: 0.23 by {Constants.Donee.FirstName} {Constants.Donee.LastName} 5 minutes ago.") { Emoji = true },
                        }
                    },
                    new Actions
                    {
                        Elements = new List<IActionElement>
                        {
                            new Button
                            {
                                Text = new TextObject("Train as :help:") { Emoji = true },
                                Style = "primary",
                                Value = "help"
                            },
                            new Button
                            {
                                Text = new TextObject("Train as :donate:") { Emoji = true },
                                Value = "donate"
                            },
                            new Button
                            {
                                Text = new TextObject("Retry :retry:") { Emoji = true },
                                Value = "retry"
                            },
                        }
                    }
                },
            }, phoneEntry);
        }

        public async Task SendAddsAutomationActions()
        {
            using var http = new HttpClient();
            using var jtc = new JoinableTaskContext();
            using var events = new EventGridStream(
                Mock.Of<IServiceProvider>(),
                environment,
                new Serializer(),
                jtc.Factory);

            var people = new TestPersonRepository();
            var phoneSystems = new TestEntityRepository<PhoneEntry>();
            var phoneThreads = new TestEntityRepository<PhoneThread>();
            var slackHandler = new SlackMessageSentHandler(environment, phoneThreads, http);

            await phoneSystems.PutAsync(new PhoneEntry(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber)
            {
                AutomationPaused = true
            });

            var handler = new SlackEventHandler(environment, people, phoneSystems, events,
                Mock.Of<ILanguageUnderstanding>(x => x.PredictAsync(It.IsAny<string>()) ==
                    Task.FromResult(new Prediction(Intents.Help, new Dictionary<string, Intent>
                    {
                        { Intents.Help, new Intent { Score = 0.55 } },
                        { Intents.Donate, new Intent { Score = 0.25 } },
                    }, new Dictionary<string, object>(), default, default))));

            SlackMessageSent sent = default;
            events.Of<SlackMessageSent>().Subscribe(e => sent = e);

            await handler.HandleAsync(new MessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, "Hey"));
            await slackHandler.HandleAsync(sent);

            await handler.HandleAsync(new MessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, "Threaded, Resume only."));
            await slackHandler.HandleAsync(sent);

            await phoneSystems.PutAsync(new PhoneEntry(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber)
            {
                AutomationPaused = false
            });

            await handler.HandleAsync(new UnknownMessageReceived(Constants.Donee.PhoneNumber, "Threaded, Pause only."));
            await slackHandler.HandleAsync(sent);
        }

        async Task SendMessageAsync(SlackMessage message, PhoneEntry phoneSystem)
        {
            using var http = new HttpClient();
            using var jtc = new JoinableTaskContext();
            using var events = new EventGridStream(
                Mock.Of<IServiceProvider>(),
                environment,
                new Serializer(),
                jtc.Factory);

            var people = new TestPersonRepository();
            var phoneSystems = new TestEntityRepository<PhoneEntry>();
            var phoneThreads = new TestEntityRepository<PhoneThread>();
            var slackHandler = new SlackMessageSentHandler(environment, phoneThreads, http);

            await phoneSystems.PutAsync(phoneSystem);

            var handler = new SlackEventHandler(environment, people, phoneSystems, events,
                Mock.Of<ILanguageUnderstanding>(x => x.PredictAsync(It.IsAny<string>()) ==
                    Task.FromResult(new Prediction(Intents.Help,
                    new Dictionary<string, Intent>(),
                    new Dictionary<string, object>(),
                    default,
                    default))));

            SlackMessageSent sent = default;
            events.Of<SlackMessageSent>().Subscribe(e => sent = e);

            await handler.SendAsync(phoneSystem.UserNumber, message, phoneSystem);

            Assert.NotNull(sent);

            var sender = new SlackMessageSentHandler(environment, phoneThreads, http);
            await sender.HandleAsync(sent);
        }
    }
}
