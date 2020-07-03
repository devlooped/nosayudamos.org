using Xunit;
using Xunit.Abstractions;

namespace NosAyudamos
{
    public class SerializerTests
    {
        ITestOutputHelper output;

        public SerializerTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void CanRoundtripRequestCreated()
        {
            var serializer = new Serializer();

            var expected = new RequestCreated(Constants.Donee.Id + "-" + Base62.Encode(PreciseTime.UtcNow.Ticks), 0, "");

            var json = serializer.Serialize(expected);

#if DEBUG
            output.WriteLine(json);
#endif

            var actual = serializer.Deserialize<RequestCreated>(json);

            Assert.Equal(expected.PersonId, actual.PersonId);
            Assert.Equal(expected.RequestId, actual.RequestId);
        }
    }
}
