namespace FoodTour.ITest.UI.Fixtures;

public sealed class FoodTourApplicationFixture : WebApplicationFactory<global::FoodTour.Program>, IAsyncLifetime {
    private readonly string connectionString;
    private readonly string databaseName;
    private bool isInitialised;
    private IHost? kestrelHost;

    public FoodTourApplicationFixture() {
        databaseName = $"FoodTourITests_{Guid.NewGuid():N}";
        connectionString = CreateConnectionString(databaseName);
        TestEmailSender = new TestEmailSender();
    }

    public string ConnectionString => connectionString;

    public string DatabaseName => databaseName;

    public Uri RootUri => ClientOptions.BaseAddress ?? throw new InvalidOperationException("The application has not been started.");

    public FoodTourSeedData SeedData { get; private set; } = null!;

    public TestEmailSender TestEmailSender { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) => {
            Dictionary<string, string?> testSettings = new() {
                ["ConnectionStrings:FoodTourContext"] = connectionString,
                ["EmailSettings:Host"] = "localhost",
                ["EmailSettings:Port"] = "25",
                ["EmailSettings:UseSsl"] = "false",
                ["EmailSettings:FromEmail"] = "itest@localhost",
                ["EmailSettings:FromName"] = "FoodTour Integration Tests"
            };

            configBuilder.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices(services => {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(TestEmailSender);
            services.AddSingleton<IEmailSender>(serviceProvider => serviceProvider.GetRequiredService<TestEmailSender>());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder) {
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder => {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("http://127.0.0.1:0");
        });

        kestrelHost = builder.Build();
        kestrelHost.Start();

        var server = kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        string? address = addresses?.Addresses.LastOrDefault();

        if (string.IsNullOrWhiteSpace(address)) {
            throw new InvalidOperationException("The application did not expose a listening address.");
        }

        ClientOptions.BaseAddress = new Uri(address);
        testHost.Start();

        return testHost;
    }

    public async ValueTask InitializeAsync() {
        if (!isInitialised) {
            using var httpClient = CreateClient();
            SeedData = await SeedDatabaseAsync();
            isInitialised = true;
        }
    }

    public override async ValueTask DisposeAsync() {
        if (kestrelHost is not null) {
            await kestrelHost.StopAsync();
            kestrelHost.Dispose();
            kestrelHost = null;
        }

        await base.DisposeAsync();
        await DeleteDatabaseAsync();
    }

    private async Task DeleteDatabaseAsync() {
        var optionsBuilder = new DbContextOptionsBuilder<FoodTourContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        await context.Database.EnsureDeletedAsync();
    }

    private async Task<FoodTourSeedData> SeedDatabaseAsync() {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
        return await FoodTourTestDataSeeder.SeedAsync(context);
    }

    private static string CreateConnectionString(string databaseName) {
        return $"Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog={databaseName};Integrated Security=SSPI;TrustServerCertificate=True;MultipleActiveResultSets=True";
    }
}
