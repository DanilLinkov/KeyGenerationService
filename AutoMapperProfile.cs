using AutoMapper;
using KeyGenerationService.Dtos;
using KeyGenerationService.Models;

namespace KeyGenerationService
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<AvailableKey, GetKeyDto>();
            CreateMap<TakenKey, GetKeyDto>();
        }
    }
}