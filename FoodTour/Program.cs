namespace FoodTour;

public class Program {
    public static void Main(string[] args) {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateBootstrapLogger();

        try {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog
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

            var app = builder.Build();

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
