using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Dtos;
using KeyGenerationService.Models;

namespace KeyGenerationService.Services
{
    public interface IKeyService
    {
        Task<GetKeyDto> GetAKeyAsync(int size);
        Task<List<GetKeyDto>> GetKeysAsync(int count, int size);
        Task ReturnKeysAsync(ReturnKeyDto keys);
    }
}