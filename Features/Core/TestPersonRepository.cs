using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class TestPersonRepository : IPersonRepository
    {
        Dictionary<string, string> phoneIdMap = new Dictionary<string, string>();
        Dictionary<string, Person> people = new Dictionary<string, Person>();

        public Task<Person> FindAsync(string phoneNumber, bool readOnly = true)
        {
            if (phoneIdMap.TryGetValue(phoneNumber, out var nationalId))
                return GetAsync(nationalId);

            return Task.FromResult(default(Person));
        }

        public Task<Person> GetAsync(string nationalId, bool readOnly = true) => Task.FromResult(people[nationalId]);

        public Task<Person> PutAsync(Person person)
        {
            phoneIdMap[person.PhoneNumber] = person.Id;
            people[person.Id] = person;
            return Task.FromResult(person);
        }
    }
}
