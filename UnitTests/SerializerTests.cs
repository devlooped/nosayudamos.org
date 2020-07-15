using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NosAyudamos
{
    public class SerializerTests
    {
        [Fact]
        public void CanSerializeToFlatDictionary()
        {
            var json = JObject.Parse(File.ReadAllText("SerializerTests.json"));

            var data = new Serializer().Deserialize(File.ReadAllText("SerializerTests.json"));

            Assert.Equal("foo@bar.com", data["collector.email"].ToString());
            Assert.Equal("22222222", data["collector.identification.number"].ToString());
            Assert.Equal("not_specified", data["payer.shipping_modes[1]"].ToString());
        }
    }
}
