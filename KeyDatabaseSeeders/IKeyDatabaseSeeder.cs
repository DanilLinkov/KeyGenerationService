using System.Threading.Tasks;

namespace KeyGenerationService.KeyDatabaseSeeders
{
    public interface IKeyDatabaseSeeder
    {
        Task GenerateAndSeedAsync(int numberOfKeys, int sizeOfKey);
    }
}