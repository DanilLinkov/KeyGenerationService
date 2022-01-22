using System.Threading.Tasks;

namespace KeyGenerationService.Auth.RateLimiters
{
    public interface IRateLimiter
    {
        Task<int> IsAllowedAsync(int keysToCreate);
    }
}