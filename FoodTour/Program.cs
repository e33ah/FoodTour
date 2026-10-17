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
                    .Enrich.With(new UkTimestampEnricher(ukTimeZoneInfo))
                    .Enrich.FromLogContext();
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add FareHarbor settings with validation
            builder.Services.AddOptions<FareHarborSettings>()
                .Bind(builder.Configuration.GetSection("FareHarbor"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Add database context
            string connectionString = builder.Configuration.GetConnectionString("FoodTourContext") ?? throw new InvalidOperationException("Connection string 'FoodTourContext' not found.");
            builder.Services.AddDbContext<FoodTourContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDbContext<DataProtectionKeyContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<DataProtectionKeyContext>()
                .SetApplicationName("FoodTour");

            // Add User Support Settings
            builder.Services.Configure<SupportSettings>(
                builder.Configuration.GetSection("SupportSettings"));

            // Add Email Settings
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            // Add Admin Notification Settings
            builder.Services.Configure<AdminNotificationSettings>(
                builder.Configuration.GetSection("AdminNotificationSettings"));

            builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
            builder.Services.AddScoped<IRazorTemplateRenderer, RazorTemplateRenderer>();
            builder.Services.AddScoped<BookingEmailService>();
            builder.Services.AddScoped<AdminNotificationService>();
            builder.Services.AddScoped<SupplierNotificationService>();
            builder.Services.AddScoped<PartnerBookingConfirmationService>();
            builder.Services.AddScoped<IBookingValidationService, BookingValidationService>();
            builder.Services.AddScoped<IPurchaseCodeValidationService, PurchaseCodeValidationService>();
            builder.Services.AddScoped<IBookingUpdateService, BookingUpdateService>();

            // Add Admin Settings and access service
            builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));
            builder.Services.AddScoped<IAdminAccessService, AdminAccessService>();
            builder.Services.AddScoped<IAuthorizationHandler, AdminOnlyAuthorizationHandler>();

            // Add Google OAuth + cookie authentication
            string? googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            string? googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            bool googleConfigured = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

            if (!googleConfigured) {
                Log.Warning("Google OAuth credentials are not configured. Admin login will be unavailable. " +
                    "Use 'dotnet user-secrets set Authentication:Google:ClientId <value>' to configure.");
            }

            var authBuilder = builder.Services.AddAuthentication(options => {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                if (googleConfigured) {
                    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                }
            })
            .AddCookie(options => {
                options.LoginPath = "/Admin/Login";
                options.AccessDeniedPath = "/Admin/AccessDenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            });

            if (googleConfigured) {
                authBuilder.AddGoogle(options => {
                    options.ClientId = googleClientId!;
                    options.ClientSecret = googleClientSecret!;
                });
            }

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy =>
                    policy.Requirements.Add(new AdminOnlyRequirement()));

            var app = builder.Build();

            _ = app.Services.GetRequiredService<IOptions<FareHarborSettings>>().Value;

            Log.Information("Starting FoodTour application...");

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
            app.UseAuthentication();
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

    private sealed class UkTimestampEnricher(TimeZoneInfo ukTimeZoneInfo) : ILogEventEnricher {
        private const string PROPERTY_NAME = "UkTimestamp";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
            var ukDateTime = TimeZoneInfo.ConvertTimeFromUtc(logEvent.Timestamp.UtcDateTime, ukTimeZoneInfo);
            var property = propertyFactory.CreateProperty(PROPERTY_NAME, ukDateTime);
            logEvent.AddOrUpdateProperty(property);
        }
    }
}
