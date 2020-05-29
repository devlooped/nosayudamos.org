using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
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
        IEventHandler<MessageReceived>,
        IEventHandler<RegistrationFailed>
    {
        readonly IEnvironment env;
        readonly IPersonRepository peopleRepo;
        readonly IEntityRepository<PhoneSystem> phoneDir;
        readonly IEventStreamAsync events;
        readonly ILanguageUnderstanding language;

        public SlackEventHandler(
            IEnvironment env, IPersonRepository peopleRepo,
            IEntityRepository<PhoneSystem> phoneDir, IEventStreamAsync events,
            ILanguageUnderstanding language)
            => (this.env, this.peopleRepo, this.phoneDir, this.events, this.language)
            = (env, peopleRepo, phoneDir, events, language);

        public async Task HandleAsync(UnknownMessageReceived e)
        {
            if (env.IsDevelopment() && !env.GetVariable("SendToSlackInDevelopment", false))
                return;

            var prediction = await language.PredictAsync(e.Body).ConfigureAwait(false);
            var context = new StringBuilder();
            Intent? intent;
            if (prediction.Intents.TryGetValue(Intents.Help, out intent) || 
                prediction.Intents.TryGetValue(Intents.Utilities.Help, out intent))
                context = context.Append(":pray: ").Append(intent.Score?.ToString("0.##", CultureInfo.CurrentCulture));

            if (prediction.Intents.TryGetValue(Intents.Donate, out intent))
                context = context.Append(":money_with_wings: ").Append(intent.Score?.ToString("0.##", CultureInfo.CurrentCulture));

            var toEmoji = e.PhoneNumber == env.GetVariable("ChatApiNumber").TrimStart('+') ? ":whatsapp:" : ":twilio:";
            var phoneSystem = await phoneDir.GetAsync(e.PhoneNumber).ConfigureAwait(false);

            var message = new SlackMessage
            {
                Username = "nosayudamos",
                Text = $":thinking_face: {e.Body}",
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
            var person = await peopleRepo.GetAsync(e.SourceId!).ConfigureAwait(false);
            if (person == null)
                return;

            var message = new SlackMessage
            {
                Username = "nosayudamos",
                Text = $":thumbsup: {person.FirstName} {person.LastName}",
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
                    }
                },
            };

            await SendAsync(person.PhoneNumber, message).ConfigureAwait(false);
        }

        public async Task HandleAsync(TaxStatusRejected e)
        {
            var person = await peopleRepo.GetAsync(e.SourceId!).ConfigureAwait(false);
            if (person == null)
                return;

            var message = new SlackMessage
            {
                Username = "nosayudamos",
                Text = $":thumbsdown: {person.FirstName} {person.LastName} {e.TaxId}",
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
                                Text = new TextObject("Approve :approved:") { Emoji = true },
                                Value = "approve"
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
            var map = await phoneDir.GetAsync(e.PhoneNumber).ConfigureAwait(false);
            // We only foward for paused phones.
            if (map == null || map.AutomationPaused != true)
                return;

            // In testing scenarios, we might get messages with local file uris, skip those.
            if (env.IsDevelopment() &&
                Uri.TryCreate(e.Body, UriKind.Absolute, out var uri) &&
                uri.Scheme == "file")
                return;

            await SendAsync(e.PhoneNumber, new SlackMessage { Text = e.Body }).ConfigureAwait(false);
        }

        public async Task HandleAsync(RegistrationFailed e)
        {
            var message = new SlackMessage
            {
                Username = "nosayudamos",
                Text = $":zap: registration failed for {e.PhoneNumber} :interrobang:",
                Blocks = new List<Block>
                {
                    new Divider(),
                    new Section
                    {
                        BlockId = "sender",
                        Fields = new List<TextObject>
                        {
                            new TextObject($":zap: {e.PhoneNumber}") { Emoji = true },
                            new TextObject($"Registration failed :interrobang:") { Emoji = true },
                        }
                    },
                },
            };

            var ngrok = env.GetVariable("StorageNgrok", default(string));
            if (env.IsTesting() && env.GetVariable("SendToSlackInDevelopment", false))
            {
                if (ngrok == null)
                {
                    message.Blocks.Add(new Section
                    {
                        Fields = new List<TextObject>
                        {
                            new TextObject($":warning: SendToSlackInDevelopment=true but StorageNgrok envvar redirecting port 10000 was not defined.") { Emoji = true },
                            new TextObject($"Attempt #1..{e.Images.Length} images can't be displayed.") { Emoji = true },
                        }
                    });
                }
                else
                {
                    message.Blocks.AddRange(e.Images.Select((uri, i) => new global::Slack.Webhooks.Blocks.Image
                    {
                        ImageUrl = new Uri(new Uri(ngrok), uri.PathAndQuery).ToString(),
                        Title = new TextObject($"Attempt #" + (i + 1)),
                        AltText = Path.GetFileName(uri.AbsolutePath),
                    }));
                }
            }
            else
            {
                message.Blocks.AddRange(e.Images.Select((uri, i) => new global::Slack.Webhooks.Blocks.Image
                {
                    ImageUrl = uri.OriginalString,
                    Title = new TextObject($"Attempt #" + (i + 1)),
                    AltText = Path.GetFileName(uri.AbsolutePath),
                }));
            }


            message.Blocks.Add(new Actions
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
            });

            await events.PushAsync(new SlackMessageSent(e.PhoneNumber, message.AsJson()));
        }

        internal async Task SendAsync(string phoneNumber, SlackMessage message, PhoneSystem? phoneSystem = default)
        {
            if (phoneSystem == null)
                phoneSystem = await phoneDir.GetAsync(phoneNumber).ConfigureAwait(false);

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
            var person = await peopleRepo.FindAsync(phoneNumber, readOnly: true).ConfigureAwait(false);
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
