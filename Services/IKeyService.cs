using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Dtos;
using KeyGenerationService.Models;

namespace KeyGenerationService.Services
{
    public interface IKeyService
    {
        Task<GetKeyDto> GetAKeyAsync();
        Task<List<GetKeyDto>> GetKeysAsync(int count);
        Task ReturnKeysAsync(ReturnKeyDto keys);
    }
}