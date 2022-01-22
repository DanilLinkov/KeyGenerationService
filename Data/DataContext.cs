using KeyGenerationService.Auth;
using KeyGenerationService.Models;
using Microsoft.EntityFrameworkCore;

namespace KeyGenerationService.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            
        }
        
        public DbSet<AvailableKey> AvailableKeys { get; set; }
        public DbSet<TakenKey> TakenKeys { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Keys
            modelBuilder.Entity<AvailableKey>().HasKey(t => t.Id);
            modelBuilder.Entity<AvailableKey>().HasAlternateKey(t => t.Key);
            
            modelBuilder.Entity<TakenKey>().HasKey(t => t.Id);
            modelBuilder.Entity<TakenKey>().HasAlternateKey(t => t.Key);
            
            modelBuilder.Entity<ApiKey>().HasKey(t => t.Id);
            modelBuilder.Entity<ApiKey>().HasAlternateKey(t => t.Key);
            
            // Columns
            modelBuilder.Entity<AvailableKey>().Property(t => t.Size).IsRequired();
            
            modelBuilder.Entity<TakenKey>().Property(t => t.Size).IsRequired();
            
            modelBuilder.Entity<ApiKey>().Property(t => t.OwnerName).IsRequired();
            modelBuilder.Entity<ApiKey>().Ignore(t => t.Claims);

            // Relationships

        }
    }
}