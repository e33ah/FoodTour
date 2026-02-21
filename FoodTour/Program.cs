namespace FoodTour;

public class Program {
    public static void Main(string[] args) {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateBootstrapLogger();

        try {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            builder.Host.UseSerilog((context, services, configuration) => {
                var ukTimeZoneInfo = GetUkTimeZoneInfo();
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.WithProperty("UkTimestamp", () => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ukTimeZoneInfo))
                    .Enrich.FromLogContext();
            });

            Log.Information("Starting FoodTour application...");

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add database context
            string connectionString = builder.Configuration.GetConnectionString("FoodTourContext") ?? throw new InvalidOperationException("Connection string 'FoodTourContext' not found.");
            builder.Services.AddDbContext<FoodTourContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDbContext<DataProtectionKeyContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<DataProtectionKeyContext>()
                .SetApplicationName("FoodTour");

            // Add Email Settings
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
            builder.Services.AddScoped<BookingEmailService>();

            var app = builder.Build();

            // Apply EF Core migrations on start-up
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FoodTourContext>();
            var dataProtectionDb = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();

            try {
                ApplyMigrations(dataProtectionDb, "Data Protection");
                ApplyMigrations(db, "Application");
            } catch (Exception ex) {
                Log.Fatal(ex, "An error occurred while applying database migrations.");
                throw new ApplicationException("Failed to apply database migrations.", ex);
            }

            Log.Information("Application started - Application: {Application}, Environment: {Environment}, MachineName: {MachineName}",
                "FoodTour",
                app.Environment.EnvironmentName,
                Environment.MachineName);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();

        } catch (Exception ex) {
            Log.Fatal(ex, "Host terminated unexpectedly");
            throw;
        } finally {
            Log.Information("Shutting down FoodTour application...");
            Log.CloseAndFlush();
        }
    }

    private static void ApplyMigrations(DbContext context, string name) {
        var pendingMigrations = context.Database.GetPendingMigrations();
        if (pendingMigrations.Any()) {
            context.Database.Migrate();
            Log.Information("{Name} database migrations applied successfully.", name);
        } else {
            Log.Information("No pending {Name} database migrations were found.", name);
        }
    }

    private static TimeZoneInfo GetUkTimeZoneInfo() {
        try {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        } catch (TimeZoneNotFoundException) {
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
    }
}
