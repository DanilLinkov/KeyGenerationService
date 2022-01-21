using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using KeyGenerationService.Data;
using KeyGenerationService.Models;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.KeyDatabaseSeeders
{
    public class KeyDatabaseSeeder: IKeyDatabaseSeeder
    {
        private readonly DataContext _dbContext;
        private readonly char[] _allowedCharacters;
        private readonly RandomNumberGenerator _randomNumberGenerator;

        public KeyDatabaseSeeder(DataContext dbContext, char[] allowedCharacters, RandomNumberGenerator randomNumberGenerator)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _allowedCharacters = allowedCharacters ?? throw new ArgumentNullException(nameof(allowedCharacters));
            _randomNumberGenerator = randomNumberGenerator ?? throw new ArgumentNullException(nameof(randomNumberGenerator));
        }
        
        public async Task GenerateAndSeedAsync(int numberOfKeys, int sizeOfKey)
        {
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
            
            await _dbContext.AvailableKeys.AddRangeAsync(keys);
            await _dbContext.SaveChangesAsync();
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