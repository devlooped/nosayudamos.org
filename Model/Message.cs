using System.Text.Json;

namespace NosAyudamos
{
    public class Message
    {
        public string From { get; set; }
        public string Body { get; set; }
        public string To { get; set; }

        public Message(string from, string body, string to) =>
            (From, Body, To) = (from, body, to);
        
        public static Message Create(string requestBody)
        {
            var json = JsonDocument.Parse(requestBody);

            return new Message
                (json.RootElement.GetProperty("From").GetString(),
                json.RootElement.GetProperty("Body").GetString(),
                json.RootElement.GetProperty("To").GetString());
        }
    }
}