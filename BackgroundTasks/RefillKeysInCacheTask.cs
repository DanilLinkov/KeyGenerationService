using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KeyGenerationService.Data;
using KeyGenerationService.KeyDatabaseSeeders;
using KeyGenerationService.Models;
using KeyGenerationService.Services.KeyCacheService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace KeyGenerationService.BackgroundTasks
{
    public class RefillKeysInCacheTask : BackgroundService
    {
        private readonly DataContext _dbContext;
        private readonly IKeyDatabaseSeeder _databaseSeeder;
        private readonly IKeyCacheService _keyCacheService;
        private readonly int _maxKeysInCache;

        public RefillKeysInCacheTask(DataContext dbContext, IKeyDatabaseSeeder databaseSeeder, IKeyCacheService keyCacheService, int maxKeysInCache)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
            _keyCacheService = keyCacheService ?? throw new ArgumentNullException(nameof(keyCacheService));
            _maxKeysInCache = maxKeysInCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var keysInCache = await _keyCacheService.GetKeys(_maxKeysInCache);

            if (keysInCache.Count == _maxKeysInCache)
            {
                return;
            }

            var keysToAdd = await _dbContext.AvailableKeys.Take(_maxKeysInCache - keysInCache.Count).ToListAsync(cancellationToken: stoppingToken);

            if (keysToAdd.Count < _maxKeysInCache)
            {
                await _databaseSeeder.GenerateAndSeedAsync(10, 8);
                keysToAdd = await _dbContext.AvailableKeys.Take(_maxKeysInCache - keysInCache.Count).ToListAsync(cancellationToken: stoppingToken);
            }
            
            var takenKeys = keysToAdd.Select(key => new TakenKeys()
            {
                Key = key.Key,
                CreationDate = key.CreationDate,
                Size = key.Size,
                TakenDate = DateTime.Now
            }).ToList();
            
            _dbContext.AvailableKeys.RemoveRange(keysToAdd);
            _dbContext.TakenKeys.AddRange(takenKeys);
            
            await _dbContext.SaveChangesAsync(stoppingToken);

            await _keyCacheService.AddKeys(takenKeys);
        }
    }
}