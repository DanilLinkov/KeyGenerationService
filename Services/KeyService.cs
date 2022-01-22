﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using KeyGenerationService.BackgroundTasks;
using KeyGenerationService.Data;
using KeyGenerationService.Dtos;
using KeyGenerationService.KeyDatabaseSeeders;
using KeyGenerationService.KeyRetrievers;
using KeyGenerationService.KeyReturners;
using KeyGenerationService.Models;
using KeyGenerationService.Services.KeyCacheService;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.Services
{
    public class KeyService : IKeyService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IKeyDatabaseSeeder _databaseSeeder;
        private readonly IKeyCacheService _keyCacheService;
        private readonly RefillKeysInCacheTask _refillKeysInCacheTask;
        private readonly IKeyRetriever _keyRetriever;
        private readonly IKeyReturner _keyReturner;

        public KeyService(IMapper mapper,DataContext context, IKeyDatabaseSeeder databaseSeeder, IKeyCacheService keyCacheService, RefillKeysInCacheTask refillKeysInCacheTask, IKeyRetriever keyRetriever, IKeyReturner keyReturner)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
            _keyCacheService = keyCacheService ?? throw new ArgumentNullException(nameof(keyCacheService));
            _refillKeysInCacheTask = refillKeysInCacheTask ?? throw new ArgumentNullException(nameof(refillKeysInCacheTask));
            _keyRetriever = keyRetriever ?? throw new ArgumentNullException(nameof(keyRetriever));
            _keyReturner = keyReturner ?? throw new ArgumentNullException(nameof(keyReturner));
        }
        
        public async Task<GetKeyDto> GetAKeyAsync()
        {
            var keysFromCache = await _keyCacheService.GetKeys(1);
            
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
            if (count > 5)
            {
                count = 5;
            }
            
            var keysFromCache = await _keyCacheService.GetKeys(count);

            var keysLeftToGet = count - keysFromCache.Count;
            
            var keys = await _keyRetriever.RetrieveKeys(keysLeftToGet);
            
            if (keys.Count != keysLeftToGet)
            {
                await _databaseSeeder.GenerateAndSeedAsync(6, 8);
                keys.AddRange(await _keyRetriever.RetrieveKeys(keysLeftToGet - keys.Count));
            }
            
            keys.AddRange(keysFromCache);

            var getKeyDtos = keys.Select(key => _mapper.Map<GetKeyDto>(key)).ToList();

            _refillKeysInCacheTask.StartAsync(CancellationToken.None);
            
            return getKeyDtos;
        }

        public async Task ReturnKeysAsync(ReturnKeyDto returnKeyDto)
        {
            await _keyReturner.ReturnKeys(returnKeyDto.Keys);
        }
    }
}