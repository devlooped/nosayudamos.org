using System.Reflection;
using System.Resources;
using AutoMapper;
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

        [Fact]
        public void CanMapPersonEntity()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new DomainProfile()));
            var mapper = config.CreateMapper();

            var person = new Person
            {
                Role = Role.Donor
            };

            var entity = mapper.Map<PersonEntity>(person);

            Assert.Equal(nameof(Role.Donor), entity.Role);

            person = mapper.Map<Person>(entity);

            Assert.Equal(Role.Donor, person.Role);
        }
    }
}
