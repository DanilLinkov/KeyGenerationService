using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeyGenerationService.Data;
using KeyGenerationService.Models;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.KeyRetrievers
{
    public class KeyRetriever : IKeyRetriever
    {
        private readonly DataContext _context;
        private readonly int _maxNumberOfKeys;

        public KeyRetriever(DataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        
        public async Task<List<TakenKey>> RetrieveKeys(int numberOfKeys)
        {
            var keys = await _context.AvailableKeys.Take(numberOfKeys).ToListAsync();
            
            var takenKeys = keys.Select(key => new TakenKey()
            {
                Key = key.Key,
                CreationDate = key.CreationDate,
                Size = key.Size,
                TakenDate = DateTime.Now
            }).ToList();
            
            _context.AvailableKeys.RemoveRange(keys);
            _context.TakenKeys.AddRange(takenKeys);
            
            await _context.SaveChangesAsync();

            return takenKeys;
        }
    }
}