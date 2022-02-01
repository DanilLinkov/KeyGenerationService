using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Models;

namespace KeyGenerationService.KeyReturners
{
    public interface IKeyReturner
    {
        Task ReturnKeysAsync(List<string> keysToReturn);
    }
}