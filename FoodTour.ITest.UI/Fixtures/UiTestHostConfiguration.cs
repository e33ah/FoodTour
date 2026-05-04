namespace FoodTour.ITest.UI.Fixtures;

public sealed class UiTestHostConfiguration {
    private const string CONFIGURATION_FILE_NAME = "appsettings.UiTests.json";
    private const string UI_TEST_DATABASE_NAME_PREFIX_CONFIGURATION_KEY = "UiTests:Database:DatabaseNamePrefix";
    private const string UI_TEST_EMAIL_FROM_ADDRESS_CONFIGURATION_KEY = "UiTests:EmailSettings:FromEmail";
    private const string UI_TEST_EMAIL_FROM_NAME_CONFIGURATION_KEY = "UiTests:EmailSettings:FromName";
    private const string UI_TEST_EMAIL_HOST_CONFIGURATION_KEY = "UiTests:EmailSettings:Host";
    private const string UI_TEST_EMAIL_PORT_CONFIGURATION_KEY = "UiTests:EmailSettings:Port";
    private const string UI_TEST_EMAIL_USE_SSL_CONFIGURATION_KEY = "UiTests:EmailSettings:UseSsl";
    private const string UI_TEST_SQL_SERVER_CONNECTION_STRING_TEMPLATE_CONFIGURATION_KEY = "UiTests:Database:SqlServerConnectionStringTemplate";

    private UiTestHostConfiguration(UiTestDatabaseConfiguration database, UiTestEmailConfiguration emailSettings) {
        Database = database;
        EmailSettings = emailSettings;
    }

    public UiTestDatabaseConfiguration Database { get; }

    public UiTestEmailConfiguration EmailSettings { get; }

    public static UiTestHostConfiguration Load() {
        ConfigurationBuilder configurationBuilder = new();
        string configurationFilePath = Path.Combine(AppContext.BaseDirectory, CONFIGURATION_FILE_NAME);
        configurationBuilder.AddJsonFile(configurationFilePath, optional: false, reloadOnChange: false);
        configurationBuilder.AddEnvironmentVariables();
        var configuration = configurationBuilder.Build();

        return Create(configuration);
    }

    public static UiTestHostConfiguration Create(IConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(configuration);

        var database = UiTestDatabaseConfiguration.Create(
            databaseNamePrefix: configuration[UI_TEST_DATABASE_NAME_PREFIX_CONFIGURATION_KEY],
            sqlServerConnectionStringTemplate: configuration[UI_TEST_SQL_SERVER_CONNECTION_STRING_TEMPLATE_CONFIGURATION_KEY]);
        var emailSettings = UiTestEmailConfiguration.Create(
            host: configuration[UI_TEST_EMAIL_HOST_CONFIGURATION_KEY],
            port: configuration[UI_TEST_EMAIL_PORT_CONFIGURATION_KEY],
            useSsl: configuration[UI_TEST_EMAIL_USE_SSL_CONFIGURATION_KEY],
            fromEmail: configuration[UI_TEST_EMAIL_FROM_ADDRESS_CONFIGURATION_KEY],
            fromName: configuration[UI_TEST_EMAIL_FROM_NAME_CONFIGURATION_KEY]);

        return new UiTestHostConfiguration(database, emailSettings);
    }
}

public sealed class UiTestEmailConfiguration {
    private const string DEFAULT_FROM_EMAIL = "itest@localhost";
    private const string DEFAULT_FROM_NAME = "FoodTour Integration Tests";
    private const string DEFAULT_HOST = "localhost";
    private const int DEFAULT_PORT = 25;
    private const bool DEFAULT_USE_SSL = false;

    private UiTestEmailConfiguration(string host, int port, bool useSsl, string fromEmail, string fromName) {
        Host = host;
        Port = port;
        UseSsl = useSsl;
        FromEmail = fromEmail;
        FromName = fromName;
    }

    public string FromEmail { get; }

    public string FromName { get; }

    public string Host { get; }

    public int Port { get; }

    public bool UseSsl { get; }

    public static UiTestEmailConfiguration Create(string? host = null, string? port = null, string? useSsl = null, string? fromEmail = null, string? fromName = null) {
        string resolvedHost = string.IsNullOrWhiteSpace(host) ? DEFAULT_HOST : host.Trim();
        string resolvedFromEmail = string.IsNullOrWhiteSpace(fromEmail) ? DEFAULT_FROM_EMAIL : fromEmail.Trim();
        string resolvedFromName = string.IsNullOrWhiteSpace(fromName) ? DEFAULT_FROM_NAME : fromName.Trim();
        bool resolvedUseSsl = !string.IsNullOrWhiteSpace(useSsl) && bool.Parse(useSsl);
        int resolvedPort = string.IsNullOrWhiteSpace(port) ? DEFAULT_PORT : int.Parse(port);

        return new UiTestEmailConfiguration(resolvedHost, resolvedPort, resolvedUseSsl, resolvedFromEmail, resolvedFromName);
    }
}
