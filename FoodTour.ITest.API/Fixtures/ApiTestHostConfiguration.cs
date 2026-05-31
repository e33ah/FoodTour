namespace FoodTour.ITest.API.Fixtures;

public sealed class ApiTestHostConfiguration {
    private const string CONFIGURATION_FILE_NAME = "appsettings.ApiTests.json";
    private const string API_TEST_DATABASE_NAME_PREFIX_CONFIGURATION_KEY = "ApiTests:Database:DatabaseNamePrefix";
    private const string API_TEST_EMAIL_FROM_ADDRESS_CONFIGURATION_KEY = "ApiTests:EmailSettings:FromEmail";
    private const string API_TEST_EMAIL_FROM_NAME_CONFIGURATION_KEY = "ApiTests:EmailSettings:FromName";
    private const string API_TEST_EMAIL_HOST_CONFIGURATION_KEY = "ApiTests:EmailSettings:Host";
    private const string API_TEST_EMAIL_PORT_CONFIGURATION_KEY = "ApiTests:EmailSettings:Port";
    private const string API_TEST_EMAIL_USE_SSL_CONFIGURATION_KEY = "ApiTests:EmailSettings:UseSsl";
    private const string API_TEST_SQL_SERVER_CONNECTION_STRING_TEMPLATE_CONFIGURATION_KEY = "ApiTests:Database:SqlServerConnectionStringTemplate";
    private const string DEFAULT_DATABASE_NAME_PREFIX = "FoodTourApiITests_";
    private const string DEFAULT_FROM_NAME = "FoodTour API Integration Tests";

    private ApiTestHostConfiguration(IntegrationTestDatabaseConfiguration database, IntegrationTestEmailConfiguration emailSettings) {
        Database = database;
        EmailSettings = emailSettings;
    }

    public IntegrationTestDatabaseConfiguration Database { get; }

    public IntegrationTestEmailConfiguration EmailSettings { get; }

    public static ApiTestHostConfiguration Load() {
        ConfigurationBuilder configurationBuilder = new();
        string configurationFilePath = Path.Combine(AppContext.BaseDirectory, CONFIGURATION_FILE_NAME);
        configurationBuilder.AddJsonFile(configurationFilePath, optional: false, reloadOnChange: false);
        configurationBuilder.AddEnvironmentVariables();
        var configuration = configurationBuilder.Build();

        var result = Create(configuration);
        return result;
    }

    public static ApiTestHostConfiguration Create(IConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(configuration);

        var database = IntegrationTestDatabaseConfiguration.Create(
            defaultDatabaseNamePrefix: DEFAULT_DATABASE_NAME_PREFIX,
            databaseNamePrefix: configuration[API_TEST_DATABASE_NAME_PREFIX_CONFIGURATION_KEY],
            sqlServerConnectionStringTemplate: configuration[API_TEST_SQL_SERVER_CONNECTION_STRING_TEMPLATE_CONFIGURATION_KEY]);
        var emailSettings = IntegrationTestEmailConfiguration.Create(
            defaultFromName: DEFAULT_FROM_NAME,
            host: configuration[API_TEST_EMAIL_HOST_CONFIGURATION_KEY],
            port: configuration[API_TEST_EMAIL_PORT_CONFIGURATION_KEY],
            useSsl: configuration[API_TEST_EMAIL_USE_SSL_CONFIGURATION_KEY],
            fromEmail: configuration[API_TEST_EMAIL_FROM_ADDRESS_CONFIGURATION_KEY],
            fromName: configuration[API_TEST_EMAIL_FROM_NAME_CONFIGURATION_KEY]);

        ApiTestHostConfiguration result = new(database, emailSettings);
        return result;
    }
}
