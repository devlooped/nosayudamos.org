using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    class ShowRegisterUIProcessor : ISlackPayloadProcessor
    {
        readonly IEnvironment environment;
        readonly HttpClient http;

        static string RegisterUI { get; }

        static ShowRegisterUIProcessor()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NosAyudamos.Slack.RegisterDonee.json");
            using var reader = new StreamReader(stream);
            RegisterUI = reader.ReadToEnd();
        }

        public ShowRegisterUIProcessor(IEnvironment environment, HttpClient http)
            => (this.environment, this.http)
            = (environment, http);

        public bool AppliesTo(JObject payload) =>
            (string?)payload["type"] == "block_actions" &&
            payload.SelectString("$.actions[0].value") == "register";

        public async Task ProcessAsync(JObject payload)
        {
            using var uirequest = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/views.open");
            uirequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", environment.GetVariable("SlackToken"));

            var ui = new JObject
            {
                { "trigger_id", (string)payload["trigger_id"]! },
                { "view", JObject.Parse(RegisterUI.Replace("$sender$", payload.GetSender(), StringComparison.Ordinal)) },
            }.ToString();

            uirequest.Content = new StringContent(ui, Encoding.UTF8, "application/json");
            var uiresponse = await http.SendAsync(uirequest);
            var data = await uiresponse.Content.ReadAsStringAsync();

            Serilog.Log.Information("{response} => {ui}", data, ui);
        }
    }
}
