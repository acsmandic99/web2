using Microsoft.EntityFrameworkCore;
using TripService.Entities;

namespace TripService.Data
{
    public class TripDbContext : DbContext
    {
        public TripDbContext(DbContextOptions<TripDbContext> options) : base(options) { }

        public DbSet<Trip> Trips => Set<Trip>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(255);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.StartDate).IsRequired();
                entity.Property(t => t.EndDate).IsRequired();
                entity.Property(t => t.EstimatedBudget).IsRequired();
                entity.Property(t => t.UserId).IsRequired();
            });
        }
    }
}