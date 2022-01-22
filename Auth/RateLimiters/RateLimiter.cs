using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KeyGenerationService.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.Auth.RateLimiters
{
    public class RateLimiter : IRateLimiter
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataContext _dataContext;

        public RateLimiter(IHttpContextAccessor httpContextAccessor, DataContext dataContext)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }
        
        public async Task<int> IsAllowedAsync(int keysToCreate)
        {
            var apiKeyString = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            var apiKey = await _dataContext.ApiKeys.FirstOrDefaultAsync(k => k.Key == apiKeyString);
            
            if (apiKey == null)
            {
                return 0;
            }

            if (apiKey.KeysCreatedToday >= apiKey.KeysAllowedToday) return 0;

            if (apiKey.KeysCreatedToday + keysToCreate > apiKey.KeysAllowedToday)
            {
                keysToCreate = apiKey.KeysAllowedToday - apiKey.KeysCreatedToday;
            }

            apiKey.KeysCreatedToday += keysToCreate;
            await _dataContext.SaveChangesAsync();

            return keysToCreate;
        }
    }
}