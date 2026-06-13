using Microsoft.EntityFrameworkCore;
using ActivityService.Entities;

namespace ActivityService.Data
{
    public class ActivityDbContext : DbContext
    {
        public ActivityDbContext(DbContextOptions<ActivityDbContext> options) : base(options) { }

        public DbSet<Activity> Activities => Set<Activity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Activity>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Name).IsRequired().HasMaxLength(255);
                entity.Property(a => a.Location).IsRequired().HasMaxLength(255);
                entity.Property(a => a.Description).HasMaxLength(1000);
                entity.Property(a => a.Status).HasMaxLength(50);
                entity.Property(a => a.ScheduledAt).IsRequired();
                entity.Property(a => a.Price).IsRequired();
                entity.Property(a => a.TripId).IsRequired();
            });
        }
    }
}