using Microsoft.EntityFrameworkCore;
using TripService.Entities;

namespace TripService.Data
{
    public class TripDbContext : DbContext
    {
        public TripDbContext(DbContextOptions<TripDbContext> options) : base(options) { }

        public DbSet<Trip> Trips => Set<Trip>();
        public DbSet<Destination> Destinations => Set<Destination>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(255);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.GeneralNotes).HasMaxLength(2000);
                entity.Property(t => t.StartDate).IsRequired();
                entity.Property(t => t.EndDate).IsRequired();
                entity.Property(t => t.EstimatedBudget).IsRequired();
                entity.Property(t => t.UserId).IsRequired();
            });

            modelBuilder.Entity<Destination>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).IsRequired().HasMaxLength(255);
                entity.Property(d => d.Location).IsRequired().HasMaxLength(255);
                entity.Property(d => d.Notes).HasMaxLength(1000);
                entity.Property(d => d.ArrivalDate).IsRequired();
                entity.Property(d => d.DepartureDate).IsRequired();
                entity.Property(d => d.TripId).IsRequired();
            });
        }
    }
}