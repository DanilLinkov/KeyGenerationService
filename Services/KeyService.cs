using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using KeyGenerationService.Auth.RateLimiters;
using KeyGenerationService.BackgroundTasks;
using KeyGenerationService.Data;
using KeyGenerationService.Dtos;
using KeyGenerationService.KeyCachers;
using KeyGenerationService.KeyDatabaseSeeders;
using KeyGenerationService.KeyRetrievers;
using KeyGenerationService.KeyReturners;
using KeyGenerationService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.Services
{
    public class KeyService : IKeyService
    {
        private readonly IRateLimiter _rateLimiter;
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IKeyDatabaseSeeder _databaseSeeder;
        private readonly IKeyCacher _keyCacher;
        private readonly RefillKeysInCacheTask _refillKeysInCacheTask;
        private readonly IKeyRetriever _keyRetriever;
        private readonly IKeyReturner _keyReturner;

        public KeyService(IRateLimiter rateLimiter, IMapper mapper,DataContext context, IKeyDatabaseSeeder databaseSeeder, RefillKeysInCacheTask refillKeysInCacheTask, IKeyCacher keyCacher, IKeyRetriever keyRetriever, IKeyReturner keyReturner)
        {
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
            _refillKeysInCacheTask = refillKeysInCacheTask ?? throw new ArgumentNullException(nameof(refillKeysInCacheTask));
            _keyCacher = keyCacher ?? throw new ArgumentNullException(nameof(keyCacher));
            _keyRetriever = keyRetriever ?? throw new ArgumentNullException(nameof(keyRetriever));
            _keyReturner = keyReturner ?? throw new ArgumentNullException(nameof(keyReturner));
        }
        
        public async Task<GetKeyDto> GetAKeyAsync()
        {
            var count = await _rateLimiter.IsAllowedAsync(1);
            
            if (count == 0)
            {
                return null;
            }

            var keysFromCache = await _keyCacher.GetKeys(1);
            
            if (keysFromCache.Count > 0)
            {
                var getCacheKeyDto = _mapper.Map<GetKeyDto>(keysFromCache.First());
            
                return getCacheKeyDto;
            }

            var keysFromDatabase = await _keyRetriever.RetrieveKeys(1);

            if (keysFromDatabase.Count <= 0)
            {
                await _databaseSeeder.GenerateAndSeedAsync(5, 8);
                keysFromDatabase = await _keyRetriever.RetrieveKeys(1);
            }
            
            var key = keysFromDatabase.First();
            
            var getKeyDto = _mapper.Map<GetKeyDto>(key);
            
            _refillKeysInCacheTask.StartAsync(CancellationToken.None);
            
            return getKeyDto;
        }

        public async Task<List<GetKeyDto>> GetKeysAsync(int count)
        {
            count = await _rateLimiter.IsAllowedAsync(count);

            if (count == 0) return null;
            if (count > 5) count = 5;

            var keysFromCache = await _keyCacher.GetKeys(count);

            if (keysFromCache.Count < count)
            {
                var keysLeftToGet = count - keysFromCache.Count;
            
                var keys = await _keyRetriever.RetrieveKeys(keysLeftToGet);
            
                if (keys.Count != keysLeftToGet)
                {
                    await _databaseSeeder.GenerateAndSeedAsync(6, 8);
                    keys.AddRange(await _keyRetriever.RetrieveKeys(keysLeftToGet - keys.Count));
                }
            
                keysFromCache.AddRange(keys);
                
                _refillKeysInCacheTask.StartAsync(CancellationToken.None);
            }

            var getKeyDtos = keysFromCache.Select(key => _mapper.Map<GetKeyDto>(key)).ToList();

            return getKeyDtos;
        }

        public async Task ReturnKeysAsync(ReturnKeyDto returnKeyDto)
        {
            await _keyReturner.ReturnKeys(returnKeyDto.Keys);
        }
    }
}