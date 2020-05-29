using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Newtonsoft.Json;
using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using Slack.Webhooks.Interfaces;

namespace NosAyudamos
{
    /// <summary>
    /// Handles multiple domain events that we want to forward to slack for further 
    /// human intervention.
    /// </summary>
    [Order(10)]
    class SlackEventHandler :
        IEventHandler<TaxStatusRejected>,
        IEventHandler<TaxStatusAccepted>,
        IEventHandler<UnknownMessageReceived>,
        IEventHandler<MessageReceived>
    {
        readonly IEnvironment environment;
        readonly IPersonRepository repository;
        readonly IEntityRepository<PhoneSystem> phoneRepo;
        readonly IEventStreamAsync events;
        readonly ILanguageUnderstanding language;

        public SlackEventHandler(
            IEnvironment environment, IPersonRepository repository,
            IEntityRepository<PhoneSystem> phoneRepo, IEventStreamAsync events,
            ILanguageUnderstanding language)
            => (this.environment, this.repository, this.phoneRepo, this.events, this.language)
            = (environment, repository, phoneRepo, events, language);

        public async Task HandleAsync(UnknownMessageReceived e)
        {
            if (environment.IsDevelopment() && !environment.GetVariable("SendToSlackInDevelopment", false))
                return;

            var intents = await language.GetIntentsAsync(e.Body).ConfigureAwait(false);
            var context = new StringBuilder();
            if (intents.TryGetValue("Help", out var help))
                context = context.Append(":pray: ").Append(help.Score?.ToString("0.##", CultureInfo.CurrentCulture));

            if (intents.TryGetValue("Donate", out var donate))
                context = context.Append(":money_with_wings: ").Append(donate.Score?.ToString("0.##", CultureInfo.CurrentCulture));

            var toEmoji = e.PhoneNumber == environment.GetVariable("ChatApiNumber").TrimStart('+') ? ":whatsapp:" : ":twilio:";
            var phoneSystem = await phoneRepo.GetAsync(e.PhoneNumber).ConfigureAwait(false);

            var message = new SlackMessage
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
                            new TextObject($":unknown: {e.PhoneNumber}") { Emoji = true },
                            new TextObject($"{toEmoji} {phoneSystem?.SystemNumber}") { Emoji = true },
                        }
                    },
                    new Section
                    {
                        BlockId = "body",
                        Text = new TextObject($"> {e.Body}") { Type = TextObject.TextType.Markdown, Emoji = true },
                    },
                    new Context
                    {
                        Elements = new List<IContextElement>
                        {
                            new TextObject(context.ToString().Trim()) { Emoji = true },
                        }
                    },
                    new Actions
                    {
                        BlockId = "actions",
                        Elements = new List<IActionElement>
                        {
                            new Button
                            {
                                Text = new TextObject("Train as :pray:") { Emoji = true },
                                Style = "primary",
                                Value = "help"
                            },
                            new Button
                            {
                                Text = new TextObject("Train as :money_with_wings:") { Emoji = true },
                                Value = "donate"
                            },
                            new Button
                            {
                                Text = new TextObject("Retry :repeat:") { Emoji = true },
                                Value = "retry"
                            },
                        }
                    }
                },
            };

            await SendAsync(e.PhoneNumber, message, phoneSystem).ConfigureAwait(false);
        }

        public async Task HandleAsync(TaxStatusAccepted e)
        {
            var person = await repository.GetAsync(e.SourceId!).ConfigureAwait(false);
            if (person == null)
                return;

            await events.PushAsync(new MessageSent(
                person.PhoneNumber,
                Strings.UI.Donee.Welcome(person.FirstName.Split(' ').First(), person.Sex == Sex.Male ? "o" : "a")))
                .ConfigureAwait(false);

            var message = new SlackMessage
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
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/search.php?q={e.SourceId}|{e.TaxId}>")
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
                                Text = new TextObject("Pause :automation_pause:") { Emoji = true },
                                Value = "pause"
                            },
                            new Button
                            {
                                Text = new TextObject("Resume :automation_resume:") { Emoji = true },
                                Value = "resume"
                            },
                        }
                    }
                },
            };

            await SendAsync(person.PhoneNumber, message).ConfigureAwait(false);
        }

        public async Task HandleAsync(TaxStatusRejected e)
        {
            var person = await repository.GetAsync(e.SourceId!).ConfigureAwait(false);
            if (person == null)
                return;

            var name = person.FirstName.Split(' ').First();
            var body = e.Reason switch
            {
                TaxStatusRejectedReason.NotApplicable => Strings.UI.Donee.NotApplicable(name),
                TaxStatusRejectedReason.HasIncomeTax => Strings.UI.Donee.HasIncomeTax(name),
                TaxStatusRejectedReason.HighCategory => Strings.UI.Donee.HighCategory(name),
                _ => Strings.UI.Donee.Rejected,
            };

            await events.PushAsync(new MessageSent(person.PhoneNumber, body)).ConfigureAwait(false);

            var message = new SlackMessage
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
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/search.php?q={e.SourceId}|{e.TaxId}>")
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
                                Text = new TextObject("Pause :automation_pause:") { Emoji = true },
                                Value = "pause"
                            },
                            new Button
                            {
                                Text = new TextObject("Resume :automation_resume:") { Emoji = true },
                                Value = "resume"
                            },
                        }
                    }
                },
            };

            await SendAsync(person.PhoneNumber, message).ConfigureAwait(false);
        }

        /// <summary>
        /// When automation is disabled for the user, we forward all messages to slack
        /// so interaction can be taken over manually.
        /// </summary>
        public async Task HandleAsync(MessageReceived e)
        {
            var map = await phoneRepo.GetAsync(e.PhoneNumber).ConfigureAwait(false);
            // We only foward for paused phones.
            if (map == null || map.AutomationPaused != true)
                return;

            await SendAsync(e.PhoneNumber, new SlackMessage { Text = e.Body }).ConfigureAwait(false);
        }

        async Task SendAsync(string phoneNumber, SlackMessage message, PhoneSystem? phoneSystem = default)
        {
            if (phoneSystem == null)
                phoneSystem = await phoneRepo.GetAsync(phoneNumber).ConfigureAwait(false);

            if (phoneSystem == null)
                return;

            if (message.Blocks == null)
            {
                // It's a simple Text message, we need to turn it into a block-based one
                message.Blocks = new List<Block>
                {
                    new Section
                    {
                        Text = new TextObject(message.Text) { Type = TextObject.TextType.PlainText, Emoji = true}
                    }
                };
            }

            var context = message.Blocks.OfType<Context>().FirstOrDefault();
            if (context == null)
            {
                context = new Context { Elements = new List<IContextElement>() };
                var actions = message.Blocks.OfType<Actions>().FirstOrDefault();
                if (actions == null)
                    message.Blocks.Add(context);
                else
                    message.Blocks.Insert(message.Blocks.IndexOf(actions), context);
            }
            var footer = context.Elements.OfType<TextObject>().FirstOrDefault();
            if (footer == null)
            {
                footer = new TextObject();
                context.Elements.Add(footer);
            }

            var by = "+" + phoneNumber;
            var person = await repository.FindAsync(phoneNumber, readOnly: true).ConfigureAwait(false);
            if (person != null)
            {
                by = person.FirstName + " " + person.LastName;
            }

            footer.Emoji = true;
            footer.Text += " by " + by;
            footer.Text += phoneSystem.AutomationPaused == true ? " :automation_paused:" : " :automation_enabled:";

            await events.PushAsync(new SlackMessageSent(phoneNumber, message.AsJson())).ConfigureAwait(false);
        }
    }
}
