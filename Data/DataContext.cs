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
        
        public DbSet<AvailableKeys> AvailableKeys { get; set; }
        public DbSet<TakenKeys> TakenKeys { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Keys
            modelBuilder.Entity<AvailableKeys>().HasKey(t => t.Id);
            modelBuilder.Entity<AvailableKeys>().HasAlternateKey(t => t.Key);
            
            modelBuilder.Entity<TakenKeys>().HasKey(t => t.Id);
            modelBuilder.Entity<TakenKeys>().HasAlternateKey(t => t.Key);
            
            modelBuilder.Entity<ApiKey>().HasKey(t => t.Id);
            modelBuilder.Entity<ApiKey>().HasAlternateKey(t => t.Key);
            
            // Columns
            modelBuilder.Entity<AvailableKeys>().Property(t => t.Size).IsRequired();
            
            modelBuilder.Entity<TakenKeys>().Property(t => t.Size).IsRequired();

            modelBuilder.Entity<ApiKey>().Property(t => t.OwnerName).IsRequired();
            
            modelBuilder.Entity<ApiKey>().Ignore(t => t.Claims);

            // Relationships

        }
    }
}