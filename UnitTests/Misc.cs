using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace NosAyudamos
{
    public class Misc
    {
        [Fact]
        public void DefaultCulture()
        {
            var culture = typeof(IMessaging).Assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>().CultureName;
            Assert.Equal("es-AR", culture);
        }

        [Fact]
        public async Task RecognizeCaptcha()
        {
            var env = new Environment();
            var key = env.GetVariable("ComputerVisionSubscriptionKey");
            var url = env.GetVariable("ComputerVisionEndpoint") + "vision/v2.1/read/core/asyncBatchAnalyze";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            HttpResponseMessage response;

            //var bytes = File.ReadAllBytes("CaptchaCode.png");
            var bytes = File.ReadAllBytes(@"LocalOnly\Content\Diana.jpg");

            using (var content = new ByteArrayContent(bytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);
            }

            if (response.IsSuccessStatusCode)
            {
                var location = response.Headers.GetValues("Operation-Location").FirstOrDefault();
                string json;
                int i = 0;
                do
                {
                    Thread.Sleep(1000);
                    response = await client.GetAsync(location);
                    json = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 10 && json.IndexOf("\"status\":\"Succeeded\"") == -1);

                if (i == 10 && json.IndexOf("\"status\":\"Succeeded\"") == -1)
                {
                    Console.WriteLine("\nTimeout error.\n");
                    return;
                }

                Console.WriteLine(json);
            }


            //var recognition = JsonConvert.DeserializeObject<TextRecognition>(json);

            //Console.WriteLine(recognition);
        }

        [SkippableFact]
        public async Task RecognizeDni()
        {
            Skip.IfNot(File.Exists(@"LocalOnly\Content\Diana.jpg"), @"Test requires a DNI at 'LocalOnly\Content\Diana.jpg'");

            var env = new Environment();
            var key = env.GetVariable("ComputerVisionSubscriptionKey");
            var url = env.GetVariable("ComputerVisionEndpoint") + "vision/v3.0-preview/read/analyze?language=es";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

                HttpResponseMessage response;

                // Read the contents of the specified local image
                // into a byte array.
                byte[] byteData = File.ReadAllBytes(@"LocalOnly\Content\Diana.jpg");

                // Add the byte array as an octet stream to the request body.
                using (var content = new ByteArrayContent(byteData))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    // Asynchronously call the REST API method.
                    response = await client.PostAsync(url, content);
                }

                // Asynchronously get the JSON response.
                var contentString = await response.Content.ReadAsStringAsync();

                var recognition = JsonConvert.DeserializeObject<VisionResult>(contentString);

                Console.WriteLine(recognition);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
            }
        }

        [Fact]
        public async Task GetEntities()
        {
            var env = new Environment();
            var language = new LanguageUnderstanding(env,
                new Resiliency(env).GetRegistry(),
                Mock.Of<ILogger<LanguageUnderstanding>>());

            var intents = await language.GetIntentsAsync("creo q es 54223");

            var entities = await language.GetEntitiesAsync("creo q es 54223");

            var numbers = JsonConvert.DeserializeObject<int[]>(entities["number"].ToString());

            Assert.Single(numbers);
            Assert.Equal(54223, numbers[0]);
        }
    }
}
