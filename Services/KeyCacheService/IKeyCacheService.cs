using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Models;

namespace KeyGenerationService.Services.KeyCacheService
{
    public interface IKeyCacheService
    {
        Task<List<TakenKeys>> GetKeys(int count);
        Task AddKeys(List<TakenKeys> keys);
        Task SetKeys(List<TakenKeys> keys);
    }
}