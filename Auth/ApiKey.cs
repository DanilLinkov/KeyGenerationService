using System;
using System.Collections.Generic;
using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;

namespace KeyGenerationService.Auth
{
    public class ApiKey : IApiKey
    {
        public int Id { get; set; }
        public string Key { get; }
        public string OwnerName { get; }
        public int KeysCreatedToday { get; set; }
        public int KeysAllowedToday { get; set; }
        public IReadOnlyCollection<Claim> Claims { get; }
    }
}