using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Models;

namespace KeyGenerationService.KeyReturners
{
    public interface IKeyReturner
    {
        Task ReturnKeys(List<string> keysToReturn);
    }
}