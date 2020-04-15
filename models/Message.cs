using System.Text.Json;

namespace NosAyudamos
{
    public class Message
    {
        public string From { get; set; }
        public string Body { get; set; }
        public string To { get; set; }

        public static Message Create(string requestBody)
        {
            var json = JsonDocument.Parse(requestBody);

            return new Message
            {
                From = json.RootElement.GetProperty("From").GetString(),
                Body= json.RootElement.GetProperty("Body").GetString(),
                To = json.RootElement.GetProperty("To").GetString()
            };
        }
    }
}