using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NosAyudamos
{
    public class TaxIdTests
    {
        [Theory]
        [InlineData("23696294", Sex.Male, "20236962947")]
        [InlineData("22718904", Sex.Female, "27227189040")]
        public void CalculateTaxId(string personalId, Sex sex, string taxId) 
            => Assert.Equal(taxId, TaxId.FromNationalId(personalId, sex));

        [Theory(Skip = "Requires manual validation of captchas")]
        [InlineData("45234079", "Agustina Paula", "Cazzulino", Sex.Female, "")]
        [InlineData("23696294", "Daniel Hector", "Cazzulino", Sex.Male, null)]
        [InlineData("22718904", "Analia Viviana", "Carvallo", Sex.Female, "A")]
        public async Task GetConstancia(string nationalId, string firstName, string lastName, Sex sex, string expectedCategory)
        {
            using var http = new HttpClient();
            var recognizer = new TaxIdRecognizer(http);

            var taxId = TaxId.FromNationalId(nationalId, sex);
            var captcha = await recognizer.GetCaptchaAsync();

            while (captcha == null)
            {
                captcha = await recognizer.GetCaptchaAsync();
                Thread.Sleep(10000);
            }

            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "captcha.gif"), captcha.Image);
            Process.Start(new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "captcha.gif"))
            {
                UseShellExecute = true
            });

            var code = "REPLACE WITH CAPTCHA";
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "captcha.txt"), code);
            Process.Start(new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "captcha.txt"))
            {
                UseShellExecute = true
            });

            while (code == "REPLACE WITH CAPTCHA")
            {
                Thread.Sleep(1000);
                code = File.ReadAllText(Path.Combine(Path.GetTempPath(), "captcha.txt"));
            }

            var id = await recognizer.RecognizeAsync(
                new Person(nationalId, firstName, lastName, "9112223333", sex: sex),
                code,
                captcha.Code);

            Assert.Equal(expectedCategory, id.Category);
        }

        [Fact]
        public async Task RecognizeNonMatchingFullname()
        {
            using var http = new HttpClient();
            var recognizer = new TaxIdRecognizer(http);

            var id = await recognizer.RecognizeAsync(
                new Person("23696294", "Foo", "Bar", "9112223333", sex: Sex.Male),
                "1234", "1234");

            Assert.Null(id);
        }

        [Fact]
        public async Task RecognizeWithExpiredCaptcha()
        {
            using var http = new HttpClient();
            var recognizer = new TaxIdRecognizer(http);

            var id = await recognizer.RecognizeAsync(
                new Person("23696294", "Daniel Hector", "Cazzulino", "9112223333", sex: Sex.Male),
                "27985", "1589554273345");

            Assert.Same(TaxId.Expired, id);
        }
    }
}
