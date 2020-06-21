using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class TestPersonRepository : IPersonRepository
    {
        Dictionary<string, (string Id, Role Role)> phoneIdMap = new Dictionary<string, (string, Role)>();
        Dictionary<string, Person> people = new Dictionary<string, Person>();

        public Task<Person> FindAsync(string phoneNumber, bool readOnly = true)
        {
            if (phoneIdMap.TryGetValue(phoneNumber, out var phoneMap))
                return Task.FromResult(people[phoneMap.Id]);

            return Task.FromResult(default(Person));
        }

        public Task<TPerson> GetAsync<TPerson>(string nationalId, bool readOnly = true) where TPerson : Person
            => Task.FromResult((TPerson)people[nationalId]);

        public Task<TPerson> PutAsync<TPerson>(TPerson person) where TPerson : Person
        {
            phoneIdMap[person.PhoneNumber] = (person.Id, person.Role);
            people[person.Id] = person;
            return Task.FromResult(person);
        }
    }
}
