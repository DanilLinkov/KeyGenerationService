using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using KeyGenerationService.Data;
using KeyGenerationService.Dtos;
using KeyGenerationService.KeyDatabaseSeeders;
using KeyGenerationService.Models;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.Services
{
    public class KeyService : IKeyService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IKeyDatabaseSeeder _databaseSeeder;

        public KeyService(IMapper mapper,DataContext context, IKeyDatabaseSeeder databaseSeeder)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
        }
        
        public async Task<GetKeyDto> GetAKeyAsync()
        {
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
            return getKeyDto;
        }

        public async Task<List<GetKeyDto>> GetKeysAsync(int count)
        {
            if (count > 5)
            {
                count = 5;
            }

            var keys = await _context.AvailableKeys.Take(count).ToListAsync();
            
            if (keys.Count != count)
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
            });
            
            _context.AvailableKeys.RemoveRange(keys);
            _context.TakenKeys.AddRange(takenKeys);
            
            await _context.SaveChangesAsync();

            var getKeyDtos = keys.Select(key => _mapper.Map<GetKeyDto>(key)).ToList();
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