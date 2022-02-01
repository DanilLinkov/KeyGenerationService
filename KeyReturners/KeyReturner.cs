using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeyGenerationService.Data;
using KeyGenerationService.Models;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.KeyReturners
{
    public class KeyReturner : IKeyReturner
    {
        private readonly DataContext _context;

        public KeyReturner(DataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task ReturnKeysAsync(List<string> keysToReturn)
        {
            var takenKeysToReturn = await _context.TakenKeys.Where(key => keysToReturn.Contains(key.Key)).ToListAsync();
            
            var availableKeys = takenKeysToReturn.Select(key => new AvailableKey()
            {
                Key = key.Key,
                CreationDate = key.CreationDate,
                Size = key.Size
            });
            
            _context.TakenKeys.RemoveRange(takenKeysToReturn);
            _context.AvailableKeys.AddRange(availableKeys);

            await _context.SaveChangesAsync();
        }
    }
}