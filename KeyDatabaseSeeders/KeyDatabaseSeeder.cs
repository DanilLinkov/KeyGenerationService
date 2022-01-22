using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using KeyGenerationService.Data;
using KeyGenerationService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KeyGenerationService.KeyDatabaseSeeders
{
    public class KeyDatabaseSeeder: IKeyDatabaseSeeder
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly char[] _allowedCharacters;
        private readonly RandomNumberGenerator _randomNumberGenerator;

        public KeyDatabaseSeeder(IServiceScopeFactory serviceScopeFactory, char[] allowedCharacters, RandomNumberGenerator randomNumberGenerator)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _allowedCharacters = allowedCharacters ?? throw new ArgumentNullException(nameof(allowedCharacters));
            _randomNumberGenerator = randomNumberGenerator ?? throw new ArgumentNullException(nameof(randomNumberGenerator));
        }
        
        public async Task GenerateAndSeedAsync(int numberOfKeys, int sizeOfKey)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            
            var keys = new AvailableKeys[numberOfKeys];
            
            for (var i = 0; i < numberOfKeys; i++)
            {
                var key = GetUniqueKey(sizeOfKey);
                
                keys[i] = new AvailableKeys
                {
                    Key = key,
                    Size = sizeOfKey,
                    CreationDate = DateTime.Now
                };
            }
            
            await dbContext.AvailableKeys.AddRangeAsync(keys);
            await dbContext.SaveChangesAsync();
        }
        
        public string GetUniqueKey(int size)
        {            
            byte[] data = new byte[4*size];
            using (var crypto = _randomNumberGenerator)
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % _allowedCharacters.Length;

                result.Append(_allowedCharacters[idx]);
            }

            return result.ToString();
        }
    }
}