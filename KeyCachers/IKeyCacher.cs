using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Models;

namespace KeyGenerationService.KeyCachers
{
    public interface IKeyCacher
    {
        Task<List<TakenKey>> GetKeys(int count, int size);
        Task AddKeys(List<TakenKey> keys, int size);
        Task SetKeys(List<TakenKey> keys, int size);
    }
}