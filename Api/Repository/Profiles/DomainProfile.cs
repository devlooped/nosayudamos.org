using AutoMapper;

namespace NosAyudamos
{
    class DomainProfile : Profile
    {
        public DomainProfile()
        {
            CreateMap<PersonEntity, Person>()
                .ForMember(person => person.NationalId, cfg => cfg.MapFrom(entity => entity.PartitionKey));

            CreateMap<Person, PersonEntity>()
                .ForMember(entity => entity.PartitionKey, cfg => cfg.MapFrom(person => person.NationalId));
        }
    }
}
