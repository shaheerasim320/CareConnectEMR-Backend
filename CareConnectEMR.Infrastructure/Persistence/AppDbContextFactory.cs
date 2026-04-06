using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CareConnectEMR.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var environmentName =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";

            var basePath = ResolveApiProjectPath();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddUserSecrets<AppDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured for EF Core design-time. Set it in user secrets, appsettings.{Environment}.json, or ConnectionStrings__DefaultConnection."
                );

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure()
            );

            return new AppDbContext(optionsBuilder.Options);
        }

        private static string ResolveApiProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var candidatePaths = new[]
            {
                Path.Combine(currentDirectory, "..", "CareConnectEMR.API"),
                Path.Combine(currentDirectory, "..", "..", "CareConnectEMR.API"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CareConnectEMR.API")
            };

            foreach (var candidatePath in candidatePaths)
            {
                var fullPath = Path.GetFullPath(candidatePath);
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            throw new InvalidOperationException(
                "Could not locate the CareConnectEMR.API project directory for EF Core design-time configuration."
            );
        }
    }
}
