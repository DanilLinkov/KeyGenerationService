using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCore.Authentication.ApiKey;
using KeyGenerationService.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.Auth
{
    public class ApiKeyProvider : IApiKeyProvider
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiKeyProvider(DataContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }
        
        public async Task<IApiKey> ProvideAsync(string key)
        {
            var apiKey = await _context.ApiKeys.Where(k => k.Key.ToLower().Equals(key.ToLower())).FirstOrDefaultAsync();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, apiKey.Key),
                new Claim(ClaimTypes.Name, apiKey.OwnerName),
            };
            
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
            
            _httpContextAccessor.HttpContext.SignInAsync(claimsPrincipal);
            
            
            return apiKey;
        }
    }
}