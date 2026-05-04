namespace FoodTour.ITest.UI.Fixtures;

public sealed class FoodTourApplicationFixture : WebApplicationFactory<global::FoodTour.Program>, IAsyncLifetime {
    private const string DEVELOPMENT_ENVIRONMENT = "Development";
    private const string EMAIL_FROM_ADDRESS_ENVIRONMENT_VARIABLE = "EmailSettings__FromEmail";
    private const string EMAIL_FROM_NAME_ENVIRONMENT_VARIABLE = "EmailSettings__FromName";
    private const string EMAIL_HOST_ENVIRONMENT_VARIABLE = "EmailSettings__Host";
    private const string EMAIL_PORT_ENVIRONMENT_VARIABLE = "EmailSettings__Port";
    private const string EMAIL_USE_SSL_ENVIRONMENT_VARIABLE = "EmailSettings__UseSsl";
    private const string LOCALHOST_DYNAMIC_PORT_URL = "http://127.0.0.1:0";
    private const string FOOD_TOUR_CONNECTION_STRING_ENVIRONMENT_VARIABLE = "ConnectionStrings__FoodTourContext";
    private readonly string connectionString;
    private readonly UiTestDatabaseConfiguration databaseConfiguration;
    private readonly string databaseName;
    private readonly Dictionary<string, string?> originalEnvironmentVariables = [];
    private readonly UiTestHostConfiguration uiTestHostConfiguration;
    private bool isInitialised;
    private IBrowser? browser;
    private IHost? kestrelHost;
    private IPlaywright? playwright;

    public FoodTourApplicationFixture() {
        uiTestHostConfiguration = UiTestHostConfiguration.Load();
        databaseConfiguration = uiTestHostConfiguration.Database;
        databaseName = databaseConfiguration.CreateDatabaseName();
        connectionString = databaseConfiguration.CreateConnectionString(databaseName);
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

    private Dictionary<string, string?> CreateTestSettings() {
        var emailSettings = uiTestHostConfiguration.EmailSettings;

        return new Dictionary<string, string?> {
            ["ConnectionStrings:FoodTourContext"] = connectionString,
            ["EmailSettings:Host"] = emailSettings.Host,
            ["EmailSettings:Port"] = emailSettings.Port.ToString(),
            ["EmailSettings:UseSsl"] = emailSettings.UseSsl.ToString().ToLowerInvariant(),
            ["EmailSettings:FromEmail"] = emailSettings.FromEmail,
            ["EmailSettings:FromName"] = emailSettings.FromName
        };
    }

    private void ApplyEnvironmentVariables() {
        var emailSettings = uiTestHostConfiguration.EmailSettings;

        var environmentVariables = new Dictionary<string, string?> {
            [FOOD_TOUR_CONNECTION_STRING_ENVIRONMENT_VARIABLE] = connectionString,
            [EMAIL_HOST_ENVIRONMENT_VARIABLE] = emailSettings.Host,
            [EMAIL_PORT_ENVIRONMENT_VARIABLE] = emailSettings.Port.ToString(),
            [EMAIL_USE_SSL_ENVIRONMENT_VARIABLE] = emailSettings.UseSsl.ToString().ToLowerInvariant(),
            [EMAIL_FROM_ADDRESS_ENVIRONMENT_VARIABLE] = emailSettings.FromEmail,
            [EMAIL_FROM_NAME_ENVIRONMENT_VARIABLE] = emailSettings.FromName
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
        string resolvedDatabaseName = context.Database.GetDbConnection().Database;

        if (!string.Equals(resolvedDatabaseName, databaseName, StringComparison.Ordinal)) {
            throw new InvalidOperationException("The UI test host did not use the isolated test database.");
        }
    }
}
