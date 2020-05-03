using System.Reflection;
using System.Resources;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace NosAyudamos
{
    public class Misc
    {
        [Fact]
        public void DefaultCulture()
        {
            var culture = typeof(IMessaging).Assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>().CultureName;
            Assert.Equal("es-AR", culture);
        }
    }
}
