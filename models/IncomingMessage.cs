using Newtonsoft.Json.Linq;

namespace NosAyudamos
{
    public class IncomingMessage
    {
        public string From { get; set; }
        public string Body { get; set; }
        public string To { get; set; }

        public static IncomingMessage Create(string requestBody)
        {
            var json = JObject.Parse(requestBody);

            return new IncomingMessage
            {
                From = json.Value<string>("From"),
                Body= json.Value<string>("Body"),
                To = json.Value<string>("To"),
            };
        }
    }
}