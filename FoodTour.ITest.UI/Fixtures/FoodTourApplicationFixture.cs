namespace FoodTour.ITest.UI.Fixtures;

public sealed class FoodTourApplicationFixture : WebApplicationFactory<global::FoodTour.Program>, IAsyncLifetime {
    private const string DATABASE_NAME_PREFIX = "FoodTourITests_";
    private const string DEVELOPMENT_ENVIRONMENT = "Development";
    private const string EMAIL_FROM_ADDRESS_ENVIRONMENT_VARIABLE = "EmailSettings__FromEmail";
    private const string EMAIL_FROM_NAME_ENVIRONMENT_VARIABLE = "EmailSettings__FromName";
    private const string EMAIL_FROM_ADDRESS = "itest@localhost";
    private const string EMAIL_FROM_NAME = "FoodTour Integration Tests";
    private const string EMAIL_HOST = "localhost";
    private const string EMAIL_HOST_ENVIRONMENT_VARIABLE = "EmailSettings__Host";
    private const string EMAIL_PORT = "25";
    private const string EMAIL_PORT_ENVIRONMENT_VARIABLE = "EmailSettings__Port";
    private const string EMAIL_USE_SSL_ENVIRONMENT_VARIABLE = "EmailSettings__UseSsl";
    private const string LOCALHOST_DYNAMIC_PORT_URL = "http://127.0.0.1:0";
    private const string FOOD_TOUR_CONNECTION_STRING_ENVIRONMENT_VARIABLE = "ConnectionStrings__FoodTourContext";
    private const string SQL_SERVER_CONNECTION_STRING_TEMPLATE = "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog={0};Integrated Security=SSPI;TrustServerCertificate=True;MultipleActiveResultSets=True";
    private readonly string connectionString;
    private readonly string databaseName;
    private readonly Dictionary<string, string?> originalEnvironmentVariables = [];
    private bool isInitialised;
    private IBrowser? browser;
    private IHost? kestrelHost;
    private IPlaywright? playwright;

    public FoodTourApplicationFixture() {
        databaseName = $"{DATABASE_NAME_PREFIX}{Guid.NewGuid():N}";
        connectionString = CreateConnectionString(databaseName);
        TestEmailSender = new TestEmailSender();
    }

    public string ConnectionString => connectionString;

    public string DatabaseName => databaseName;

    public Uri RootUri => ClientOptions.BaseAddress ?? throw new InvalidOperationException("The application has not been started.");

    public FoodTourSeedData SeedData { get; private set; } = null!;

    public TestEmailSender TestEmailSender { get; }

    public async Task<IBrowserContext> CreateBrowserContextAsync() {
        if (browser is null) {
            throw new InvalidOperationException("The Playwright browser has not been initialised.");
        }

        return await browser.NewContextAsync(new BrowserNewContextOptions {
            BaseURL = RootUri.ToString()
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment(DEVELOPMENT_ENVIRONMENT);
        ConfigureTestServices(builder);
    }

    protected override IHost CreateHost(IHostBuilder builder) {
        builder.ConfigureAppConfiguration((_, configBuilder) => {
            configBuilder.AddInMemoryCollection(CreateTestSettings());
        });

        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder => {
            webHostBuilder.UseEnvironment(DEVELOPMENT_ENVIRONMENT);
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls(LOCALHOST_DYNAMIC_PORT_URL);
        });

        kestrelHost = builder.Build();
        kestrelHost.Start();

        var server = kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        string? address = addresses?.Addresses.LastOrDefault();

        if (string.IsNullOrWhiteSpace(address)) {
            throw new InvalidOperationException("The application did not expose a listening address.");
        }

        ValidateKestrelHostUsesTestDatabase();
        ClientOptions.BaseAddress = new Uri(address);
        testHost.Start();

        return testHost;
    }

    private void ConfigureTestServices(IWebHostBuilder builder) {
        builder.ConfigureServices(services => {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(TestEmailSender);
            services.AddSingleton<IEmailSender>(serviceProvider => serviceProvider.GetRequiredService<TestEmailSender>());
        });
    }

    public async ValueTask InitializeAsync() {
        if (!isInitialised) {
            await PrepareDatabaseAsync();
            ApplyEnvironmentVariables();
            using var httpClient = CreateClient();
            SeedData = await SeedDatabaseAsync();
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
                Headless = true
            });
            isInitialised = true;
        }
    }

    public override async ValueTask DisposeAsync() {
        if (browser is not null) {
            await browser.CloseAsync();
            browser = null;
        }

        playwright?.Dispose();
        playwright = null;

        if (kestrelHost is not null) {
            await kestrelHost.StopAsync();
            kestrelHost.Dispose();
            kestrelHost = null;
        }

        await base.DisposeAsync();
        await DeleteDatabaseAsync();
        RestoreEnvironmentVariables();
    }

    private async Task DeleteDatabaseAsync() {
        var optionsBuilder = new DbContextOptionsBuilder<FoodTourContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        await context.Database.EnsureDeletedAsync();
    }

    private async Task PrepareDatabaseAsync() {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
    }

    private async Task<FoodTourSeedData> SeedDatabaseAsync() {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
        return await FoodTourTestDataSeeder.SeedAsync(context);
    }

    private static string CreateConnectionString(string databaseName) {
        return string.Format(SQL_SERVER_CONNECTION_STRING_TEMPLATE, databaseName);
    }

    private Dictionary<string, string?> CreateTestSettings() {
        return new Dictionary<string, string?> {
            ["ConnectionStrings:FoodTourContext"] = connectionString,
            ["EmailSettings:Host"] = EMAIL_HOST,
            ["EmailSettings:Port"] = EMAIL_PORT,
            ["EmailSettings:UseSsl"] = "false",
            ["EmailSettings:FromEmail"] = EMAIL_FROM_ADDRESS,
            ["EmailSettings:FromName"] = EMAIL_FROM_NAME
        };
    }

    private void ApplyEnvironmentVariables() {
        var environmentVariables = new Dictionary<string, string?> {
            [FOOD_TOUR_CONNECTION_STRING_ENVIRONMENT_VARIABLE] = connectionString,
            [EMAIL_HOST_ENVIRONMENT_VARIABLE] = EMAIL_HOST,
            [EMAIL_PORT_ENVIRONMENT_VARIABLE] = EMAIL_PORT,
            [EMAIL_USE_SSL_ENVIRONMENT_VARIABLE] = "false",
            [EMAIL_FROM_ADDRESS_ENVIRONMENT_VARIABLE] = EMAIL_FROM_ADDRESS,
            [EMAIL_FROM_NAME_ENVIRONMENT_VARIABLE] = EMAIL_FROM_NAME
        };

        foreach ((string key, string? value) in environmentVariables) {
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

    private void ValidateKestrelHostUsesTestDatabase() {
        if (kestrelHost is null) {
            throw new InvalidOperationException("The Kestrel host has not been started.");
        }

        using var serviceScope = kestrelHost.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<FoodTourContext>();
        string resolvedConnectionString = context.Database.GetConnectionString() ?? string.Empty;

        if (!resolvedConnectionString.Contains(databaseName, StringComparison.Ordinal)) {
            throw new InvalidOperationException("The UI test host did not use the isolated test database.");
        }
    }
}
