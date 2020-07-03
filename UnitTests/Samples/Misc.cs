using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using ZXing;
using ZXing.PDF417;

namespace NosAyudamos
{
    /// <summary>
    /// Sample invocations that must be run explicitly using the TD.NET ad-hoc runner 
    /// in order to execute them. This is intentional since they are typically for 
    /// excercising actual code that may invoke live services or storage (whether 
    /// emulated or live).
    /// </summary>
    public class Misc
    {
        public async Task SendCustomEventGridEvent()
        {
            var env = new Environment();
            var domain = new Uri(env.GetVariable("EventGridUrl")).Host;
            var credentials = new TopicCredentials(env.GetVariable("EventGridAccessKey"));
            using var client = new EventGridClient(credentials);

            await client.PublishEventsAsync(domain, new List<EventGridEvent>
            {
                new EventGridEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Topic = "NosAyudamos.Inbox",
                    EventType = "NosAyudamos.Messaging",
                    EventTime = DateTime.Now,
                    Subject = "23696294",
                    Data = "{ \"body\": \"ayuda\" }",
                    DataVersion = "1.0",
                }
            });
        }

        public void GetAssemblyDefaultCulture()
        {
            var culture = typeof(IMessaging).Assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>().CultureName;
            Assert.Equal("es-AR", culture);
        }

        public async Task GetEntitiesFromLuis()
        {
            var env = new Environment();
            var language = new LanguageUnderstanding(env,
                new Resiliency(env).GetRegistry(),
                Mock.Of<ILogger<LanguageUnderstanding>>());

            var prediction = await language.PredictAsync("creo q es 54223");

            var numbers = JsonConvert.DeserializeObject<int[]>(prediction.Entities["number"].ToString());

            Assert.Single(numbers);
            Assert.Equal(54223, numbers[0]);
        }

        public void GenerateAndRecognizeBarcode()
        {
            var reader = new BarcodeReader()
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new ZXing.Common.DecodingOptions()
                {
                    TryHarder = true,
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.PDF_417 }
                },
            };

            var data = $"00000000000@{Constants.Donee.LastName}@{Constants.Donee.FirstName}@M@{Constants.Donee.Id}@A@{Constants.Donee.DateOfBirth:dd/MM/yyyy}@26/10/1986";

            var writer = new BarcodeWriterGeneric
            {
                Format = BarcodeFormat.PDF_417,
                Options = new PDF417EncodingOptions
                {
                    Height = 60,
                    Width = 240,
                    Margin = 10
                }
            };
            var bitmap = writer.WriteAsBitmap(data);

            var elements = reader.Decode(bitmap);

            Assert.Equal(data, elements.Text);
        }

        public async Task SaveGenericAndDomainEventsToTableStorage()
        {
            var env = new Environment();
            var serializer = new Serializer();
            var client = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
            var table = client.GetTableReference(nameof(SaveGenericAndDomainEventsToTableStorage));
            await table.DeleteIfExistsAsync();
            await table.CreateIfNotExistsAsync();

            await table.ExecuteAsync(TableOperation.Insert(
                new MessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, "Hola")
                    .ToEventGrid(serializer).ToEntity()));

            var person = Constants.Donee.Create();
            person.UpdatePhoneNumber(Constants.Donee2.PhoneNumber);

            foreach (var e in person.Events)
            {
                await table.ExecuteAsync(TableOperation.Insert(e.ToEntity(serializer)));
            }
        }
    }
}
