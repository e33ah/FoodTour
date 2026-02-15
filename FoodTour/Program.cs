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
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            });

            Log.Information("Starting FoodTour application...");

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add database context
            string connectionString = builder.Configuration.GetConnectionString("FoodTourContext") ?? throw new InvalidOperationException("Connection string 'FoodTourContext' not found.");
            builder.Services.AddDbContext<FoodTourContext>(options =>
                options.UseSqlServer(connectionString));

            // Define a persistent location for the key ring
            string keyPath = Path.Combine(builder.Environment.ContentRootPath, "keys");

            // Configure Data Protection to use that folder and a shared application name
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
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

            try {
                var pendingMigrations = db.Database.GetPendingMigrations();

                if (pendingMigrations.Any()) {
                    db.Database.Migrate();
                    Log.Information("Database migrations applied successfully.");
                } else {
                    Log.Information("No pending database migrations.");
                }
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
}
