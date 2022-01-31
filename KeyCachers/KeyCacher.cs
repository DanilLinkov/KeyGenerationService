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
        private readonly Func<string, int, string> _buildCacheKey;

        public KeyCacher(IDistributedCache cache, string cacheKey, Func<string, int, string> buildCacheKey)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));
            _buildCacheKey = buildCacheKey ?? throw new ArgumentNullException(nameof(buildCacheKey));
        }
        
        public async Task<List<TakenKey>> GetKeys(int count, int size)
        {
            var fullCacheKey = _buildCacheKey(_cacheKey, size);
            
            var value = await _cache.GetStringAsync(fullCacheKey);

            if (value == null)
            {
                return new List<TakenKey>();
            }

            var keys = JsonConvert.DeserializeObject<List<TakenKey>>(value);

            if (keys.Count < count)
            {
                await _cache.RemoveAsync(fullCacheKey);
                return keys;
            }

            var keysToReturn = keys.GetRange(0, count);
                    
            keys.RemoveRange(0, count);
            await SetKeys(keys, size);

            return keysToReturn;
        }

        public async Task AddKeys(List<TakenKey> keys, int size)
        {
            var fullCacheKey = _buildCacheKey(_cacheKey, size);
            
            ValidateKeys(keys, size);
            
            var value = await _cache.GetStringAsync(fullCacheKey);

            if (value == null)
            {
                await SetKeys(keys, size);
            }
            else
            {
                var existingKeys = JsonConvert.DeserializeObject<List<TakenKey>>(value);
                existingKeys.AddRange(keys);
            
                await SetKeys(existingKeys, size);
            }
        }

        public async Task SetKeys(List<TakenKey> keys, int size)
        {
            var fullCacheKey = _buildCacheKey(_cacheKey, size);
            
            ValidateKeys(keys, size);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };
            
            await _cache.SetStringAsync(fullCacheKey, JsonConvert.SerializeObject(keys), options);
        }
        
        private void ValidateKeys(List<TakenKey> keys, int size)
        {
            if (!keys.TrueForAll(k => k.Size == size && k.Key.Length == size))
            {
                throw new Exception("Keys are not valid");
            }
        }
    }
}