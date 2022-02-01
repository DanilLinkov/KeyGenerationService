using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Models;

namespace KeyGenerationService.KeyCachers
{
    public interface IKeyCacher
    {
        Task<List<TakenKey>> GetKeysAsync(int count, int size);
        Task AddKeysAsync(List<TakenKey> keys, int size);
        Task SetKeysAsync(List<TakenKey> keys, int size);
    }
}