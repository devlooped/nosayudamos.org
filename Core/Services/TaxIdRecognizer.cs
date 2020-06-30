using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sgml;

namespace NosAyudamos
{
    class TaxIdRecognizer : ITaxIdRecognizer
    {
        static readonly Random random = new Random();
        const string LookupUrl = "https://www.cuitonline.com/search.php?q=";
        const string AfipUrl = "https://seti.afip.gob.ar";
        const string ConstanciaUrl = AfipUrl + "/padron-puc-constancia-internet/jsp/Constancia.jsp";
        const string CaptchaUrl = AfipUrl + "/padron-puc-constancia-internet/images/CaptchaCode.gif?bar=";

        readonly IEnvironment env;
        readonly HttpClient http;
        readonly ILogger<TaxIdRecognizer> logger;

        public TaxIdRecognizer(IEnvironment env, HttpClient http, ILogger<TaxIdRecognizer> logger)
        {
            this.env = env;
            this.http = http;
            this.logger = logger;

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
        /// <returns>A code and its associated captcha value for verification, or <see langword="null"/>
        /// if the site for tax information validation and retrieval is down.
        /// </returns>
        async Task<(string bar, string value)?> GetCaptchaAsync()
        {
            string? value;
            string? bar;

            do
            {
                var xml = await ReadXmlAsync(await http.GetAsync(ConstanciaUrl)).ConfigureAwait(false);
                bar = (string)xml.CreateNavigator().Evaluate("string(//INPUT[@id='bar']/@value)");
                // AFIP site is down, can't do anything
                if (string.IsNullOrEmpty(bar))
                    return default;

                var image = await http.GetByteArrayAsync(CaptchaUrl + bar + random.NextDouble());
                value = await RecognizeCaptchaAsync(image);

                // We should have recognized at least a 5 digit captcha. It may 
                // become longer/more complex, but never shorter, IMO.
            } while (value != null && value.Length < 5);

            if (bar == null || value == null)
                return null;

            return (bar, value);
        }

        public async Task<TaxId?> RecognizeAsync(Person person)
        {
            var tax = await LookupAsync(person.PersonId);

            // If we got something from the quick lookup that can be sufficient to 
            // approve/deny status quickly, shortcircuit.
            if (person.CanUpdateTaxStatus(tax))
                return tax;

            // We can't continue since the next part is for CUITs only.
            if (tax.Kind == TaxIdKind.CUIL)
                return tax;

            var taxId = TaxId.FromNationalId(person.PersonId, person.Sex);

            var xml = await PostFormAsync(new Dictionary<string, string>
            {
                { "cuit", taxId},
            });

            // Site is down, can't get any content.
            if (xml == null)
                return null;

            var options = xml.CreateNavigator().Select("//select[@name='tipoCertificado']//option[@value!='']/@value")
                .OfType<XPathItem>().Select(x => x.Value).ToArray();

            // If there are multiple form choices, query them all and merge the results
            if (options.Length > 0)
            {
                TaxId? mergedTaxId = default;
                foreach (var option in options)
                {
                    xml = await PostFormAsync(new Dictionary<string, string>
                    {
                        { "cuit", taxId},
                        { "tipoCertificado", option },
                    });

                    // Site went down
                    if (xml == null)
                        return null;

                    mergedTaxId = ProcessCertificate(taxId, xml, mergedTaxId);
                }

                return mergedTaxId;
            }
            else
            {
                return ProcessCertificate(taxId, xml);
            }
        }

        async Task<XDocument?> PostFormAsync(Dictionary<string, string> values)
        {
            XDocument? xml = default;

            do
            {
                var captcha = await GetCaptchaAsync();
                if (captcha == null)
                    return null;

                var (bar, value) = captcha.Value;
                values["captchaField"] = value;
                values["bar"] = bar;

                using var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://seti.afip.gob.ar/padron-puc-constancia-internet/ConstanciaAction.do?bar=" + bar)
                {
                    Content = new FormUrlEncodedContent(values)
                };

                var result = await http.SendAsync(request);
                if (!result.IsSuccessStatusCode)
                    return null;

                xml = await ReadXmlAsync(result);

                // Check for site down, which results in an empty body response
                if (xml.Root.Element("html")?.Element("body")?.Value.Trim().Length == 0)
                    return null;

            } while (xml != null && IsCaptchaExpired(xml));

            return xml;
        }

        static bool IsCaptchaExpired(XDocument xml) => xml
            .CreateNavigator()
            .Select("//div[@class='alert' and @role='alert']/text()")
            .OfType<XPathItem>().Select(x => x.Value)
            .Any(x =>
                x.Contains("El código de seguridad se ha vencido. Intente nuevamente.", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("Código de seguridad inválido.", StringComparison.OrdinalIgnoreCase));

        static async Task<XDocument> ReadXmlAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            using (var reader = new SgmlReader(new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
            {
                reader.InputStream = new StringReader("<page>" + body + "</page>");
                using (var nons = new NoNamespaceXmlReader(reader))
                {
                    return XDocument.Load(nons);
                }
            }
        }

        TaxId? ProcessCertificate(string taxId, XDocument xml, TaxId? existing = default)
        {
            // Check for non-registered person
            var invalid = $"La CUIT {taxId} es invalida";
            var none = xml.Root.Descendants("P").Any(p => p.Value.Trim() == "La clave ingresada no es una CUIT") ||
                 xml.CreateNavigator()
                    .Select("//div[@class='alert' and @role='alert']/text()")
                    .OfType<XPathItem>().Select(x => x.Value)
                    .Any(x => x.Trim().Contains(invalid, StringComparison.Ordinal));

            if (none)
                return TaxId.None;

            var pageTitle = xml.Root.Element("HTML")?.Element("HEAD")?.Element("TITLE")?.Value;
            var isMonotributo = pageTitle?.Contains("monotributo", StringComparison.OrdinalIgnoreCase);

            TaxCategory? category = default;
            if (isMonotributo == true)
            {
                var catElement = xml.Root.Descendants("FONT").Where(font =>
                    font.Value.Contains("CATEGOR", StringComparison.OrdinalIgnoreCase) &&
                    "CATEGORÍA".Equals(WebUtility.HtmlDecode(font.Value.Trim()), StringComparison.Ordinal))
                    .FirstOrDefault();

                if (catElement != null &&
                    Enum.TryParse<TaxCategory>(catElement.Parent.Elements().Last().Value.Trim(), true, out var tc))
                    category = tc;
            }
            else
            {
                category = TaxCategory.NotApplicable;
            }

            var hasIncome = xml.CreateNavigator().Select("//TABLE/TR/TD/FONT/text()")
                .OfType<XPathItem>()
                .Select(x => x.Value.Trim())
                .Any(x => x.StartsWith("GANANCIAS PERSONAS", StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (category != null && category != TaxCategory.NotApplicable)
                    existing.Category = category.Value;
                if (hasIncome)
                    existing.HasIncomeTax = hasIncome;

                return existing;
            }
            else
            {
                return new TaxId(taxId, category, TaxIdKind.CUIT)
                {
                    HasIncomeTax = hasIncome
                };
            }
        }

        async Task<string?> RecognizeCaptchaAsync(byte[] image)
        {
            var key = env.GetVariable("ComputerVisionSubscriptionKey");
            var url = env.GetVariable("ComputerVisionEndpoint") + "vision/v3.0-preview/read/analyze?language=es";

            using var postRequest = new HttpRequestMessage(HttpMethod.Post, url);
            postRequest.Headers.Add("Ocp-Apim-Subscription-Key", key);

            using var content = new ByteArrayContent(image);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            postRequest.Content = content;

            var response = await http.SendAsync(postRequest);
            if (!response.IsSuccessStatusCode ||
                !response.Headers.GetValues("Operation-Location").Any())
            {
                logger.LogError("Failed to process captcha using vision endpoint: {0}", response.ReasonPhrase);
                return null;
            }

            var location = response.Headers.GetValues("Operation-Location").First();
            string json;
            int i = 0;
            do
            {
                await Task.Delay(1000);
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, location);
                getRequest.Headers.Add("Ocp-Apim-Subscription-Key", key);
                response = await http.SendAsync(getRequest);
                json = await response.Content.ReadAsStringAsync();
                ++i;
            }
            while (i < 10 && json.IndexOf("\"status\":\"succeeded\"", StringComparison.Ordinal) == -1);

            var result = JsonConvert.DeserializeObject<VisionResult>(json);
            var challenge = result.AnalyzeResult.ReadResults
                .SelectMany(r => r.Lines.SelectMany(l => l.Words))
                .Select(w => w.Text)
                .FirstOrDefault();

            if (challenge == null)
            {
                logger.LogError("Could not get a recognized word from captcha :(");
                return null;
            }

            return challenge;
        }

        /// <summary>
        /// Attempts a quick lookup via the cuitonline.com site which is more reliable than the 
        /// official AFIP site which is often down. This might not succeed for non-cached responses 
        /// from previous CUIT lookups, so we cannot rely on it when it doesn't find a result, but 
        /// we *can* rely on the data it *does* return.
        /// </summary>
        async Task<TaxId> LookupAsync(string nationalId)
        {
            var page = await http.GetStringAsync(LookupUrl + nationalId).ConfigureAwait(true);

            XDocument xml;
            using (var reader = new SgmlReader(new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
            {
                reader.InputStream = new StringReader(page);
                using (var nons = new NoNamespaceXmlReader(reader))
                {
                    xml = XDocument.Load(nons);
                }
            }

            var nav = xml.CreateNavigator();
            var hit = nav.SelectSingleNode("//div[@class='hit']");

            if (hit == null)
                return TaxId.Unknown;

            var kindValue = hit
                .Select("div[@class='doc-facets']/span[@class='linea-cuit-persona']/text()")
                .OfType<XPathItem>()
                .Select(x => WebUtility.HtmlDecode(x.Value).Trim().TrimEnd(':').Trim())
                .Where(x => x.Length > 0)
                .FirstOrDefault();

            TaxIdKind? kind = null;
            if (kindValue != null && Enum.TryParse<TaxIdKind>(kindValue, true, out var tik))
                kind = tik;

            var name = hit.Evaluate("string(div[@class='denominacion']//span[@class='denominacion']/text())");
            var id = (string)hit.Evaluate("string(div[@class='doc-facets']/span[@class='linea-cuit-persona']/span[@class='cuit']/text())");
            id = id.Replace("-", "", StringComparison.Ordinal);

            TaxCategory? category = default;
            var mono = hit.SelectSingleNode("div[@class='doc-facets']/span[@class='linea-monotributo-persona']");
            if (mono != null)
            {
                var content = string.Join(" ", mono.Select("text()").OfType<XPathItem>()
                    .Select(x => WebUtility.HtmlDecode(x.Value).Trim())).Trim();

                if (Enum.TryParse<TaxCategory>(content.Split(' ').Last().Trim(), out var tc))
                    category = tc;
            }

            var result = new TaxId(id, category, kind);

            var facets = hit
                .Select("div[@class='doc-facets']/text()")
                .OfType<XPathItem>()
                .Select(x => WebUtility.HtmlDecode(x.Value).Trim().TrimEnd('(', ')').Trim())
                .Where(x => x.Length > 0);

            if (facets.Any(x => x.StartsWith("ganancias", StringComparison.OrdinalIgnoreCase)))
                // We don't just set it to false if we didn't find one, because 
                // the person might still pay income taxes, but it didn't show up 
                // in the quick lookup page.
                result.HasIncomeTax = true;

            return result;
        }
    }

    interface ITaxIdRecognizer
    {
        Task<TaxId?> RecognizeAsync(Person person);
    }
}
