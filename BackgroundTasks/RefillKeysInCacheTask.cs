using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public RefillKeysInCacheTask(IServiceScopeFactory serviceScopeFactory, IKeyDatabaseSeeder databaseSeeder, IKeyCacher keyCacher, int maxKeysInCache)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
            _keyCacher = keyCacher ?? throw new ArgumentNullException(nameof(keyCacher));
            _maxKeysInCache = maxKeysInCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var keyRetriever = scope.ServiceProvider.GetRequiredService<IKeyRetriever>();
            
            var keysInCache = await _keyCacher.GetKeys(_maxKeysInCache);

            if (keysInCache.Count == _maxKeysInCache)
            {
                return;
            }

            var keysToAdd = await keyRetriever.RetrieveKeys(_maxKeysInCache - keysInCache.Count);

            if (keysToAdd.Count < _maxKeysInCache)
            {
                await _databaseSeeder.GenerateAndSeedAsync(10, 8);
                keysToAdd.AddRange(await keyRetriever.RetrieveKeys(_maxKeysInCache - keysInCache.Count));
            }

            await _keyCacher.AddKeys(keysToAdd);
        }
    }
}