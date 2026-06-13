using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;


namespace ActivityService.Data
{
    public class ActivityDbContextFactory : IDesignTimeDbContextFactory<ActivityDbContext>
    {
        public ActivityDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<ActivityDbContext>();
            optionsBuilder.UseSqlServer(connectionString, providerOptions => providerOptions.EnableRetryOnFailure());

            return new ActivityDbContext(optionsBuilder.Options);
        }
    }
}