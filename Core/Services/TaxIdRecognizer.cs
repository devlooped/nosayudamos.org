using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Sgml;

namespace NosAyudamos
{
    public class Captcha
    {
        public Captcha(string code, byte[] image)
        {
            Code = code;
            Image = image;
        }

        public string Code { get; }
        public byte[] Image { get; }
    }

    public partial class TaxId
    {
        /// <summary>
        /// The code used to generate the captcha has expired. A new 
        /// captcha must be regenerated and validated.
        /// </summary>
        public static TaxId Expired { get; } = new TaxId("", null);
        /// <summary>
        /// The person does not have a tax identification number.
        /// </summary>
        public static TaxId None { get; } = new TaxId("", "");
        /// <summary>
        /// The site to retrieve tax information is down.
        /// </summary>
        public static TaxId SiteDown { get; } = new TaxId("", null);

        public TaxId(string id, string? category)
        {
            Id = id;
            Category = category;
        }

        public string Id { get; }
        public string? Category { get; }
    }

    class TaxIdRecognizer
    {
        static readonly Random random = new Random();
        const string LookupUrl = "https://www.cuitonline.com/search.php?q=";
        const string AfipUrl = "https://seti.afip.gob.ar";
        const string ConstanciaUrl = AfipUrl + "/padron-puc-constancia-internet/jsp/Constancia.jsp";
        const string CaptchaUrl = AfipUrl + "/padron-puc-constancia-internet/images/CaptchaCode.gif?bar=";

        readonly HttpClient http;

        public TaxIdRecognizer(HttpClient http)
        {
            this.http = http;

            http.DefaultRequestHeaders.Remove("User-Agent");
            http.DefaultRequestHeaders.Remove("Origin");
            http.DefaultRequestHeaders.Remove("Referer");

            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            http.DefaultRequestHeaders.Add("Origin", "https://www.cuitonline.com");
            http.DefaultRequestHeaders.Add("Referer", "https://www.cuitonline.com/constancia/inscripcion");
        }

        /// <summary>
        /// Gets a captcha to use for requests related to tax information.
        /// </summary>
        /// <returns>A code and its associated captcha for verification, or <see langword="null"/>
        /// if the site for tax information validation and retrieval is down.
        /// </returns>
        public async Task<Captcha?> GetCaptchaAsync()
        {
            var page = await http.GetStringAsync(ConstanciaUrl).ConfigureAwait(false);

            using var reader = new SgmlReader(new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            reader.InputStream = new StringReader("<page>" + page + "</page>");
            var xml = XDocument.Load(reader);

            var bar = xml.Descendants("INPUT")
                .Where(input => input.Attribute("id")?.Value == "bar")
                .Select(input => input.Attribute("value")?.Value)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(bar))
                return null;

            var image = await http.GetByteArrayAsync(CaptchaUrl + bar + random.NextDouble());

            return new Captcha(bar, image);
        }

        public async Task<TaxId?> RecognizeAsync(Person person, string captcha, string code)
        {
            var taxId = TaxId.FromNationalId(person.Id, person.Sex);

            var page = await http.GetStringAsync(LookupUrl + taxId).ConfigureAwait(true);
            XDocument xml;

            using (var reader = new SgmlReader(new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
            {
                reader.InputStream = new StringReader("<page>" + page + "</page>");
                xml = XDocument.Load(reader);
            }

            var title = xml.Descendants("{http://www.w3.org/1999/xhtml}div")
                .Where(div => div.Attribute("class")?.Value == "denominacion")
                .Descendants("{http://www.w3.org/1999/xhtml}a")
                .Select(d => d.Attribute("title")?.Value).FirstOrDefault();

            if (string.IsNullOrEmpty(title))
                return TaxId.None;

            var contributorFullName = string.Join(' ', title
                .Split(' ')
                .Where(word => word.All(c => char.IsUpper(c)))
                .Select(word => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(word.ToLower(CultureInfo.CurrentCulture)))
                .ToList());

            // They should match. Is this an invalid scenario?
            if (!contributorFullName.Equals(person.LastName + " " + person.FirstName, StringComparison.OrdinalIgnoreCase))
                return null;

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://seti.afip.gob.ar/padron-puc-constancia-internet/ConstanciaAction.do?bar=" + code);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "cuit", taxId},
                { "captchaField", captcha},
                { "bar", code},
            });

            var result = await http.SendAsync(request);
            if (!result.IsSuccessStatusCode)
                return TaxId.SiteDown;

            var body = await result.Content.ReadAsStringAsync();

            using (var reader = new SgmlReader(new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
            {
                reader.InputStream = new StringReader("<page>" + body + "</page>");
                xml = XDocument.Load(reader);
            }

            // Check for site down, which results in an empty body response

            // Check for non-registered person
            var none = xml.Root.Descendants("P").Where(p => p.Value.Trim() == "La clave ingresada no es una CUIT").Any();
            if (none)
                return TaxId.None;

            // Check for expired captcha
            var expired = xml.Root.Descendants("div")
                .Where(div =>
                    div.Attribute("class").Value == "alert" &&
                    div.Attribute("role").Value == "alert")
                .Where(div => div.Value.Contains("El código de seguridad se ha vencido. Intente nuevamente.", StringComparison.OrdinalIgnoreCase))
                .Any();

            if (expired)
                return TaxId.Expired;

            var pageTitle = xml.Root.Element("HTML")?.Element("HEAD")?.Element("TITLE")?.Value;
            var isMonotributo = pageTitle?.Contains("monotributo", StringComparison.OrdinalIgnoreCase);

            if (isMonotributo == true)
            {
                var categoria = xml.Root.Descendants("FONT").Where(font =>
                    font.Value.Contains("CATEGOR", StringComparison.OrdinalIgnoreCase) &&
                    "CATEGORÍA".Equals(WebUtility.HtmlDecode(font.Value.Trim()), StringComparison.Ordinal))
                    .FirstOrDefault();

                if (categoria != null)
                    return new TaxId(taxId, categoria.Parent.Elements().Last().Value.Trim());
            }

            return new TaxId(taxId, null);
        }
    }
}
