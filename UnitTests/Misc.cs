using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using ZXing;
using ZXing.PDF417;

namespace NosAyudamos
{
    public class Misc
    {
        ITestOutputHelper output;

        public Misc(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void EncodeIds()
        {
            var since = new DateTimeOffset(2020, 3, 20, 0, 0, 0, TimeSpan.Zero);
            var delta = (DateTimeOffset.UtcNow.AddYears(30)  - since);

            // In 30 years' time, we'd still have plenty to go
            Assert.True(delta.TotalSeconds < int.MaxValue);

            // Sample of encoding just the seconds
            output.WriteLine(Base62.Encode((long)delta.TotalSeconds));
            // vs encoding total ticks
            output.WriteLine(Base62.Encode(delta.Ticks));

            // Final sample of encoding using person version too.
            output.WriteLine(new RequestCreated(Constants.Donee.Id, 0, "", personVersion: 5).RequestId);
            output.WriteLine(new RequestCreated(Constants.Donee.Id, 0, "", personVersion: 6).RequestId);
            output.WriteLine(new RequestCreated(Constants.Donee.Id, 0, "", personVersion: 7).RequestId);
        }

        [Fact]
        public void DefaultCulture()
        {
            var culture = typeof(IMessaging).Assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>().CultureName;
            Assert.Equal("es-AR", culture);
        }

        [Fact]
        public async Task GetEntities()
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

        [Fact]
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
    }
}
