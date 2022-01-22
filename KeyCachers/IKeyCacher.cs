using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Models;

namespace KeyGenerationService.KeyCachers
{
    public interface IKeyCacher
    {
        Task<List<TakenKey>> GetKeys(int count);
        Task AddKeys(List<TakenKey> keys);
        Task SetKeys(List<TakenKey> keys);
    }
}