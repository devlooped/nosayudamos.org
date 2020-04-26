using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class FeaturePersonRepository : IPersonRepository
    {
        Dictionary<string, string> phoneIdMap = new Dictionary<string, string>();
        Dictionary<string, Person> people = new Dictionary<string, Person>();

        public Task<Person> FindAsync(string phoneNumber)
        {
            if (phoneIdMap.TryGetValue(phoneNumber, out var nationalId))
                return GetAsync(nationalId);

            return Task.FromResult(default(Person));
        }

        public Task<Person> GetAsync(string nationalId) => Task.FromResult(people[nationalId]);

        public Task<Person> PutAsync(Person person)
        {
            phoneIdMap[person.PhoneNumber] = person.NationalId;
            people[person.NationalId] = person;
            return Task.FromResult(person);
        }
    }
}
