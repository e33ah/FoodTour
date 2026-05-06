namespace FoodTour.ITest.API.Fixtures;

public sealed class ApiApplicationFixture : WebApplicationFactory<global::FoodTour.Program>, IAsyncLifetime {
    private const string DEVELOPMENT_ENVIRONMENT = "Development";
    private const string EMAIL_FROM_ADDRESS_ENVIRONMENT_VARIABLE = "EmailSettings__FromEmail";
    private const string EMAIL_FROM_NAME_ENVIRONMENT_VARIABLE = "EmailSettings__FromName";
    private const string EMAIL_HOST_ENVIRONMENT_VARIABLE = "EmailSettings__Host";
    private const string EMAIL_PORT_ENVIRONMENT_VARIABLE = "EmailSettings__Port";
    private const string EMAIL_USE_SSL_ENVIRONMENT_VARIABLE = "EmailSettings__UseSsl";
    private const string FOOD_TOUR_CONNECTION_STRING_ENVIRONMENT_VARIABLE = "ConnectionStrings__FoodTourContext";
    private readonly string connectionString;
    private readonly string databaseName;
    private readonly ApiTestHostConfiguration hostConfiguration;
    private readonly Dictionary<string, string?> originalEnvironmentVariables = [];
    private bool isInitialised;

    public ApiApplicationFixture() {
        hostConfiguration = ApiTestHostConfiguration.Load();
        databaseName = hostConfiguration.Database.CreateDatabaseName();
        connectionString = hostConfiguration.Database.CreateConnectionString(databaseName);
        TestEmailSender = new TestEmailSender();
    }

    public string ConnectionString => connectionString;

    public string DatabaseName => databaseName;

    public FoodTourSeedData SeedData { get; private set; } = null!;

    public TestEmailSender TestEmailSender { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment(DEVELOPMENT_ENVIRONMENT);
        builder.ConfigureServices(services => {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(TestEmailSender);
            services.AddSingleton<IEmailSender>(serviceProvider => serviceProvider.GetRequiredService<TestEmailSender>());
        });
    }

    public async ValueTask InitializeAsync() {
        if (!isInitialised) {
            await PrepareDatabaseAsync();
            SeedData = await SeedDatabaseAsync();
            ApplyEnvironmentVariables();

            using var client = CreateApiClient();
            ValidateHostUsesTestDatabase();

            isInitialised = true;
        }
    }

    public override async ValueTask DisposeAsync() {
        await base.DisposeAsync();
        await DeleteDatabaseAsync();
        TestEmailSender.Clear();
        RestoreEnvironmentVariables();
    }

    public HttpClient CreateApiClient() {
        WebApplicationFactoryClientOptions clientOptions = new() {
            AllowAutoRedirect = false
        };

        var result = CreateClient(clientOptions);
        return result;
    }

    public FoodTourContext CreateDbContext() {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        FoodTourContext result = new(optionsBuilder.Options);
        return result;
    }

    private async Task DeleteDatabaseAsync() {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        await using FoodTourContext context = new(optionsBuilder.Options);
        await context.Database.EnsureDeletedAsync();
    }

    private async Task PrepareDatabaseAsync() {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        await using FoodTourContext context = new(optionsBuilder.Options);
        await context.Database.MigrateAsync();
    }

    private async Task<FoodTourSeedData> SeedDatabaseAsync() {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        await using FoodTourContext context = new(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var result = await FoodTourTestDataSeeder.SeedAsync(context);
        return result;
    }

    private void ApplyEnvironmentVariables() {
        var emailSettings = hostConfiguration.EmailSettings;

        Dictionary<string, string?> result = new() {
            [FOOD_TOUR_CONNECTION_STRING_ENVIRONMENT_VARIABLE] = connectionString,
            [EMAIL_HOST_ENVIRONMENT_VARIABLE] = emailSettings.Host,
            [EMAIL_PORT_ENVIRONMENT_VARIABLE] = emailSettings.Port.ToString(),
            [EMAIL_USE_SSL_ENVIRONMENT_VARIABLE] = emailSettings.UseSsl.ToString().ToLowerInvariant(),
            [EMAIL_FROM_ADDRESS_ENVIRONMENT_VARIABLE] = emailSettings.FromEmail,
            [EMAIL_FROM_NAME_ENVIRONMENT_VARIABLE] = emailSettings.FromName
        };

        foreach ((string key, string? value) in result) {
            if (!originalEnvironmentVariables.ContainsKey(key)) {
                originalEnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private void RestoreEnvironmentVariables() {
        foreach ((string key, string? value) in originalEnvironmentVariables) {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private void ValidateHostUsesTestDatabase() {
        using var serviceScope = Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<FoodTourContext>();
        string resolvedDatabaseName = context.Database.GetDbConnection().Database;

        if (!string.Equals(resolvedDatabaseName, databaseName, StringComparison.Ordinal)) {
            throw new InvalidOperationException("The API test host did not use the isolated test database.");
        }
    }
}
