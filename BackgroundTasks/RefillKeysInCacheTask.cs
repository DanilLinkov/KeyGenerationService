using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KeyGenerationService.BackgroundTasks.BackgroundTaskQueues;
using KeyGenerationService.Data;
using KeyGenerationService.KeyCachers;
using KeyGenerationService.KeyDatabaseSeeders;
using KeyGenerationService.KeyRetrievers;
using KeyGenerationService.Models;
using KeyGenerationService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KeyGenerationService.BackgroundTasks
{
    public class RefillKeysInCacheTask : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IKeyDatabaseSeeder _databaseSeeder;
        private readonly IKeyCacher _keyCacher;
        private readonly int _maxKeysInCache;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly int _keysToGenerateOnEmptyCache;

        public RefillKeysInCacheTask(IServiceScopeFactory serviceScopeFactory, IKeyDatabaseSeeder databaseSeeder, IKeyCacher keyCacher, int maxKeysInCache, IBackgroundTaskQueue backgroundTaskQueue, int keysToGenerateOnEmptyCache)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
            _keyCacher = keyCacher ?? throw new ArgumentNullException(nameof(keyCacher));
            _maxKeysInCache = maxKeysInCache;
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _keysToGenerateOnEmptyCache = keysToGenerateOnEmptyCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            
            var sizes = _backgroundTaskQueue.DequeueAllAsync(stoppingToken).GetAsyncEnumerator(stoppingToken);
            
            while (await sizes.MoveNextAsync())
            {
                var currentSize = sizes.Current;
                
                var keyRetriever = scope.ServiceProvider.GetRequiredService<IKeyRetriever>();
            
                var keysInCache = await _keyCacher.GetKeys(_maxKeysInCache, currentSize);

                if (keysInCache.Count == _maxKeysInCache)
                {
                    return;
                }

                var keysToAdd = await keyRetriever.RetrieveKeys(_maxKeysInCache - keysInCache.Count, currentSize);

                if (keysToAdd.Count < _maxKeysInCache)
                {
                    await _databaseSeeder.GenerateAndSeedAsync(_keysToGenerateOnEmptyCache, sizes.Current);
                    keysToAdd.AddRange(await keyRetriever.RetrieveKeys(_maxKeysInCache - keysInCache.Count, currentSize));
                }

                await _keyCacher.AddKeys(keysToAdd, sizes.Current);
            }
        }
    }
}