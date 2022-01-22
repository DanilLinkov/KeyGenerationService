using System.Collections.Generic;
using System.Threading.Tasks;
using KeyGenerationService.Models;

namespace KeyGenerationService.KeyRetrievers
{
    public interface IKeyRetriever
    {
        public Task<List<TakenKey>> RetrieveKeys(int numberOfKeys);
    }
}