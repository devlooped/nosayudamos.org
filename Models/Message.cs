using System.Text.Json;

namespace NosAyudamos
{
    public class Message
    {
        public string? From { get; set; }
        public string? Body { get; set; }
        public string? To { get; set; }

        public Message(string from, string body, string to) => (From, Body, To) = (from, body, to);

        internal Message() { }

        public static Message Create(string json) => JsonSerializer.Deserialize<Message>(json, new JsonSerializerOptions 
        {
            AllowTrailingCommas = true, 
            PropertyNameCaseInsensitive = true 
        });
    }
}
