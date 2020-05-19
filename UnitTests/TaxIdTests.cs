using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NosAyudamos
{
    public class TaxIdTests
    {
        [Theory]
        [InlineData("22611647", Sex.Female, "27226116473")]
        [InlineData("23696294", Sex.Male, "20236962947")]
        [InlineData("20023377", Sex.Male, "20200233779")]
        [InlineData("28162083", Sex.Male, "20281620836")]
        [InlineData("22718904", Sex.Female, "27227189040")]
        [InlineData("30082615", Sex.Female, "27300826151")]
        public void CalculateTaxId(string personalId, Sex sex, string taxId) 
            => Assert.Equal(taxId, TaxId.FromNationalId(personalId, sex));

        [Theory]
        [InlineData("47165853", "", "", Sex.Female, null, TaxIdKind.None, null)]
        [InlineData("45234079", "", "", Sex.Female, null, TaxIdKind.CUIL, null)]
        [InlineData("23696294", "", "", Sex.Male, null, TaxIdKind.CUIT, true)]
        [InlineData("22718904", "", "", Sex.Female, TaxCategory.A, TaxIdKind.CUIT, false)]
        [InlineData("20189078", "", "", Sex.Male, TaxCategory.NotApplicable, TaxIdKind.CUIT, false)]
        [InlineData("25188539", "", "", Sex.Male, TaxCategory.D, TaxIdKind.CUIT, null)]
        
        public async Task GetTaxStatus(
            string nationalId, string firstName, string lastName, Sex sex, 
            TaxCategory? category, TaxIdKind? kind, bool? hasIncomeTax)
        {
            var env = new Environment();
            using var http = new HttpClient();
            var recognizer = new TaxIdRecognizer(env, http, Mock.Of<ILogger<TaxIdRecognizer>>());

            var id = await recognizer.RecognizeAsync(
                new Person(nationalId, firstName, lastName, "9112223333", sex: sex));

            if (category != null)
                Assert.Equal(category, id.Category);

            if (kind != null)
                Assert.Equal(kind, id.Kind);

            if (hasIncomeTax != null)
                Assert.Equal(hasIncomeTax, id.HasIncomeTax);
        }
    }
}
