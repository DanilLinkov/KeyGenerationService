using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KeyGenerationService.BackgroundTasks;
using KeyGenerationService.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KeyGenerationService.KeyCachers
{
    public class KeyCacher : IKeyCacher
    {
        private readonly IDistributedCache _cache;
        private readonly RefillKeysInCacheTask _refillKeysInCacheTask;
        private readonly string _cacheKey;

        public KeyCacher(IDistributedCache cache, string cacheKey)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));
        }
        
        public async Task<List<TakenKey>> GetKeys(int count)
        {
            var value = await _cache.GetStringAsync(_cacheKey);

            if (value == null)
            {
                return new List<TakenKey>();
            }

            var keys = JsonConvert.DeserializeObject<List<TakenKey>>(value);

            if (keys.Count < count)
            {
                await _cache.RemoveAsync(_cacheKey);
                return keys;
            }

            var keysToReturn = keys.GetRange(0, count);
                    
            keys.RemoveRange(0, count);
            await SetKeys(keys);

            return keysToReturn;
        }

        public async Task AddKeys(List<TakenKey> keys)
        {
            var value = await _cache.GetStringAsync(_cacheKey);

            if (value == null)
            {
                await SetKeys(keys);
            }
            else
            {
                var existingKeys = JsonConvert.DeserializeObject<List<TakenKey>>(value);
                existingKeys.AddRange(keys);
            
                await SetKeys(existingKeys);
            }
        }

        public async Task SetKeys(List<TakenKey> keys)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };
            
            await _cache.SetStringAsync(_cacheKey, JsonConvert.SerializeObject(keys), options);
        }
    }
}