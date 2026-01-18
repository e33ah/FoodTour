namespace FoodTour;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FoodTourContext> {
    public FoodTourContext CreateDbContext(string[] args) {
        string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        string? connectionString = configuration.GetConnectionString("FoodTourContext");

        if (string.IsNullOrWhiteSpace(connectionString)) {
            throw new InvalidOperationException(
                "The connection string 'FoodTourContext' is not configured. " +
                "Ensure it is defined in appsettings.json, an environment-specific appsettings file, " +
                "or provided via environment variables before running Entity Framework Core design-time operations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<FoodTourContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new FoodTourContext(optionsBuilder.Options);
    }
}
