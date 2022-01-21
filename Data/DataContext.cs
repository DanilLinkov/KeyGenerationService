using KeyGenerationService.Models;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Keys
            modelBuilder.Entity<AvailableKeys>().HasKey(t => t.Id);
            modelBuilder.Entity<AvailableKeys>().HasAlternateKey(t => t.Key);
            
            modelBuilder.Entity<TakenKeys>().HasKey(t => t.Id);
            modelBuilder.Entity<TakenKeys>().HasAlternateKey(t => t.Key);
            
            // Columns
            modelBuilder.Entity<AvailableKeys>().Property(t => t.Size).IsRequired();
            
            modelBuilder.Entity<TakenKeys>().Property(t => t.Size).IsRequired();

            // Relationships

        }
    }
}