using AutoMapper;
using KeyGenerationService.Dtos;
using KeyGenerationService.Models;

namespace KeyGenerationService
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<AvailableKeys, GetKeyDto>();
            CreateMap<TakenKeys, GetKeyDto>();
        }
    }
}