using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using KeyGenerationService.BackgroundTasks;
using KeyGenerationService.Data;
using KeyGenerationService.Dtos;
using KeyGenerationService.KeyDatabaseSeeders;
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

        public KeyService(IMapper mapper,DataContext context, IKeyDatabaseSeeder databaseSeeder, IKeyCacheService keyCacheService, RefillKeysInCacheTask refillKeysInCacheTask)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
            _keyCacheService = keyCacheService ?? throw new ArgumentNullException(nameof(keyCacheService));
            _refillKeysInCacheTask = refillKeysInCacheTask ?? throw new ArgumentNullException(nameof(refillKeysInCacheTask));
        }
        
        public async Task<GetKeyDto> GetAKeyAsync()
        {
            var keysFromCache = await _keyCacheService.GetKeys(1);
            
            if (keysFromCache.Count > 0)
            {
                var getCacheKeyDto = _mapper.Map<GetKeyDto>(keysFromCache.First());
            
                return getCacheKeyDto;
            }

            var key = await _context.AvailableKeys.FirstOrDefaultAsync();

            if (key == null)
            {
                await _databaseSeeder.GenerateAndSeedAsync(5, 8);
                key = await _context.AvailableKeys.FirstOrDefaultAsync();
            }

            var takenKey = new TakenKeys()
            {
                Key = key.Key,
                CreationDate = key.CreationDate,
                Size = key.Size,
                TakenDate = DateTime.Now
            };

            _context.AvailableKeys.Remove(key);
            _context.TakenKeys.Add(takenKey);
            
            await _context.SaveChangesAsync();
            
            var getKeyDto = _mapper.Map<GetKeyDto>(takenKey);
            
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

            var keys = await _context.AvailableKeys.Take(keysLeftToGet).ToListAsync();
            
            if (keys.Count != keysLeftToGet)
            {
                await _databaseSeeder.GenerateAndSeedAsync(6, 8);
                keys = await _context.AvailableKeys.Take(count).ToListAsync();
            }
            
            var takenKeys = keys.Select(key => new TakenKeys()
            {
                Key = key.Key,
                CreationDate = key.CreationDate,
                Size = key.Size,
                TakenDate = DateTime.Now
            }).ToList();
            
            _context.AvailableKeys.RemoveRange(keys);
            _context.TakenKeys.AddRange(takenKeys);
            
            await _context.SaveChangesAsync();
            
            takenKeys.AddRange(keysFromCache);

            var getKeyDtos = takenKeys.Select(key => _mapper.Map<GetKeyDto>(key)).ToList();

            _refillKeysInCacheTask.StartAsync(CancellationToken.None);
            
            return getKeyDtos;
        }

        public async Task ReturnKeysAsync(ReturnKeyDto returnKeyDto)
        {
            var keysToReturn = await _context.TakenKeys.Where(key => returnKeyDto.Keys.Contains(key.Key)).ToListAsync();
            
            var availableKeys = keysToReturn.Select(key => new AvailableKeys()
            {
                Key = key.Key,
                CreationDate = key.CreationDate,
                Size = key.Size
            });
            
            _context.TakenKeys.RemoveRange(keysToReturn);
            _context.AvailableKeys.AddRange(availableKeys);

            await _context.SaveChangesAsync();
        }
    }
}