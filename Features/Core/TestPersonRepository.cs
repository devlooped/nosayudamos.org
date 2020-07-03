using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class TestPersonRepository : IPersonRepository
    {
        Dictionary<string, (string Id, Role Role)> phoneIdMap = new Dictionary<string, (string, Role)>();
        Dictionary<string, object> people = new Dictionary<string, object>();

        public Task<Person> FindAsync(string phoneNumber, bool readOnly = true)
        {
            if (phoneIdMap.TryGetValue(phoneNumber, out var phoneMap))
                return Task.FromResult((Person)people[phoneMap.Id]);

            return Task.FromResult(default(Person));
        }

        public Task<TPerson> GetAsync<TPerson>(string nationalId, bool readOnly = true) where TPerson : Person
        {
            object person = default;
            people.TryGetValue(nationalId, out person);
            return Task.FromResult((TPerson)person);
        }

        public Task<TPerson> PutAsync<TPerson>(TPerson person) where TPerson : Person
        {
            phoneIdMap[person.PhoneNumber] = (person.PersonId, person.Role);
            people[person.PersonId] = person;
            return Task.FromResult(person);
        }
    }
}
