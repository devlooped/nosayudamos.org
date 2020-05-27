using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.Threading;
using Moq;
using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using Slack.Webhooks.Interfaces;
using Xunit;

namespace NosAyudamos
{
    public class SlackTests
    {
        TestEnvironment environment;

        public SlackTests() => environment = new TestEnvironment();

        [Fact]
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
                            new TextObject($":rejected: 5491159278282") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/constancia/inscripcion/20251885398|CUIT sin Monotributo>")
                            {
                                Type = TextObject.TextType.Markdown
                            },
                        }
                    },
                    new Actions
                    {
                        BlockId = "actions",
                        Elements = new List<IActionElement>
                        {
                            new Button
                            {
                                Text = new TextObject("Register :register_donee:") { Emoji = true },
                                Value = "register"
                            },
                        }
                    }
                },
            });

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
                            new TextObject($":rejected: 5491159278282") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/constancia/inscripcion/20251885398|CUIT paga ganancias>")
                            {
                                Type = TextObject.TextType.Markdown
                            },
                        }
                    },
                },
            });

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
                            new TextObject($":rejected: 5491159278282") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/constancia/inscripcion/20251885398|Monotributo categoría D>")
                            {
                                Type = TextObject.TextType.Markdown
                            },
                        }
                    },
                },
            });
        }

        [Fact]
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
            });
        }

        [Fact]
        public async Task SendUnknown()
        {
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
            });
        }

        [Fact]
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
            var phoneSystems = new TestEntityRepository<PhoneSystem>();
            var phoneThreads = new TestEntityRepository<PhoneThread>();
            var slackHandler = new SlackMessageSentHandler(environment, phoneThreads, http);

            await phoneSystems.PutAsync(new PhoneSystem(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber)
            {
                AutomationPaused = true
            });

            var handler = new SlackEventHandler(environment, people, phoneSystems, events,
                Mock.Of<ILanguageUnderstanding>(x => x.GetIntentsAsync(It.IsAny<string>()) ==
                    Task.FromResult<IDictionary<string, Intent>>(new Dictionary<string, Intent>
                    {
                        { "Help", new Intent { Score = 0.55 } },
                        { "Donate", new Intent { Score = 0.25 } },
                    })));

            SlackMessageSent sent = default;
            events.Of<SlackMessageSent>().Subscribe(e => sent = e);

            await handler.HandleAsync(new MessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, "Hey"));
            await slackHandler.HandleAsync(sent);

            await handler.HandleAsync(new MessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, "Threaded, Resume only."));
            await slackHandler.HandleAsync(sent);

            await phoneSystems.PutAsync(new PhoneSystem(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber)
            {
                AutomationPaused = false
            });

            await handler.HandleAsync(new UnknownMessageReceived(Constants.Donee.PhoneNumber, "Threaded, Pause only."));
            await slackHandler.HandleAsync(sent);
        }

        async Task SendMessageAsync(SlackMessage message)
        {
            using var http = new HttpClient();
            var handler = new SlackMessageSentHandler(environment,
                new EntityRepository<PhoneThread>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer()),
                http);

            await handler.HandleAsync(new SlackMessageSent(Constants.Donee.PhoneNumber, "{ \"text\": \"Got it!\" }"));

            await handler.HandleAsync(new SlackMessageSent(Constants.Donee.PhoneNumber, message.AsJson()));
            // Subsequent one replies to the prior one.
            await handler.HandleAsync(new SlackMessageSent(Constants.Donee.PhoneNumber, "{ \"text\": \"Got it!\" }"));
        }
    }
}
