using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    static class SlackExtensions
    {
        const string UserInfoUrl = "https://slack.com/api/users.info?user=";

        public static string? SelectString(this JObject json, string path)
            => (string?)json.SelectToken(path);

        public static string? GetSender(this JObject json)
        {
            var sender = json.SelectString("$.message.blocks[?(@.block_id == 'sender')].fields[0].text");
            if (sender == null)
                sender = json.SelectString("$.view.blocks[?(@.block_id == 'sender')].text.text");

            if (!string.IsNullOrEmpty(sender) && sender.IndexOf(':', StringComparison.Ordinal) != -1)
                sender = sender.Substring(sender.LastIndexOf(':') + 1);

            return sender?.Trim();
        }

        public static async Task<string> ResolveUserAsync(this HttpClient http, IEnvironment env, string userId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoUrl + userId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", env.GetVariable("SlackToken"));
            var response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return userId;

            dynamic user = JObject.Parse(await response.Content.ReadAsStringAsync());
            var realName = (string?)user.user.real_name;
            var email = (string?)user.user?.profile?.email;

            return realName == null ? userId : email + " (" + realName + ")";
        }
    }
}
