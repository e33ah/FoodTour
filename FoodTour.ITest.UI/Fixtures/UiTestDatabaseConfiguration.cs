namespace FoodTour.ITest.UI.Fixtures;

public sealed class UiTestDatabaseConfiguration {
    private const string DATABASE_NAME_PLACEHOLDER = "{0}";
    private const string DATABASE_NAME_PREFIX_ENVIRONMENT_VARIABLE = "FoodTourITest__DatabaseNamePrefix";
    private const string DEFAULT_DATABASE_NAME_PREFIX = "FoodTourITests_";
    private const string DEFAULT_SQL_SERVER_CONNECTION_STRING_TEMPLATE = "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog={0};Integrated Security=SSPI;TrustServerCertificate=True;MultipleActiveResultSets=True";
    private const string SQL_SERVER_CONNECTION_STRING_TEMPLATE_ENVIRONMENT_VARIABLE = "FoodTourITest__SqlServerConnectionStringTemplate";

    private UiTestDatabaseConfiguration(string databaseNamePrefix, string sqlServerConnectionStringTemplate) {
        DatabaseNamePrefix = databaseNamePrefix;
        SqlServerConnectionStringTemplate = sqlServerConnectionStringTemplate;
    }

    public string DatabaseNamePrefix { get; }

    public string SqlServerConnectionStringTemplate { get; }

    public static UiTestDatabaseConfiguration Create(string? databaseNamePrefix = null, string? sqlServerConnectionStringTemplate = null) {
        string resolvedDatabaseNamePrefix = string.IsNullOrWhiteSpace(databaseNamePrefix)
            ? DEFAULT_DATABASE_NAME_PREFIX
            : databaseNamePrefix.Trim();
        string resolvedSqlServerConnectionStringTemplate = string.IsNullOrWhiteSpace(sqlServerConnectionStringTemplate)
            ? DEFAULT_SQL_SERVER_CONNECTION_STRING_TEMPLATE
            : sqlServerConnectionStringTemplate.Trim();

        if (!resolvedSqlServerConnectionStringTemplate.Contains(DATABASE_NAME_PLACEHOLDER, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"The UI test SQL Server connection string template must contain the {DATABASE_NAME_PLACEHOLDER} database-name placeholder.");
        }

        return new UiTestDatabaseConfiguration(resolvedDatabaseNamePrefix, resolvedSqlServerConnectionStringTemplate);
    }

    public static UiTestDatabaseConfiguration CreateFromEnvironment() {
        return Create(
            Environment.GetEnvironmentVariable(DATABASE_NAME_PREFIX_ENVIRONMENT_VARIABLE),
            Environment.GetEnvironmentVariable(SQL_SERVER_CONNECTION_STRING_TEMPLATE_ENVIRONMENT_VARIABLE));
    }

    public string CreateConnectionString(string databaseName) {
        if (string.IsNullOrWhiteSpace(databaseName)) {
            throw new InvalidOperationException("The UI test database name cannot be empty.");
        }

        return string.Format(SqlServerConnectionStringTemplate, databaseName);
    }

    public string CreateDatabaseName() {
        return $"{DatabaseNamePrefix}{Guid.NewGuid():N}";
    }
}
