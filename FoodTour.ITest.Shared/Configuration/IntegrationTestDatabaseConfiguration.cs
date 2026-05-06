namespace FoodTour.ITest.Shared.Configuration;

public sealed class IntegrationTestDatabaseConfiguration {
    private const string DATABASE_NAME_PLACEHOLDER = "{0}";
    private const string DEFAULT_SQL_SERVER_CONNECTION_STRING_TEMPLATE = "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog={0};Integrated Security=SSPI;TrustServerCertificate=True;MultipleActiveResultSets=True";

    private IntegrationTestDatabaseConfiguration(string databaseNamePrefix, string sqlServerConnectionStringTemplate) {
        DatabaseNamePrefix = databaseNamePrefix;
        SqlServerConnectionStringTemplate = sqlServerConnectionStringTemplate;
    }

    public string DatabaseNamePrefix { get; }

    public string SqlServerConnectionStringTemplate { get; }

    public static IntegrationTestDatabaseConfiguration Create(string defaultDatabaseNamePrefix, string? databaseNamePrefix = null, string? sqlServerConnectionStringTemplate = null) {
        if (string.IsNullOrWhiteSpace(defaultDatabaseNamePrefix)) {
            throw new InvalidOperationException("The default integration-test database-name prefix cannot be empty.");
        }

        string resolvedDatabaseNamePrefix = string.IsNullOrWhiteSpace(databaseNamePrefix)
            ? defaultDatabaseNamePrefix.Trim()
            : databaseNamePrefix.Trim();
        string resolvedSqlServerConnectionStringTemplate = string.IsNullOrWhiteSpace(sqlServerConnectionStringTemplate)
            ? DEFAULT_SQL_SERVER_CONNECTION_STRING_TEMPLATE
            : sqlServerConnectionStringTemplate.Trim();

        if (!resolvedSqlServerConnectionStringTemplate.Contains(DATABASE_NAME_PLACEHOLDER, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"The integration-test SQL Server connection string template must contain the {DATABASE_NAME_PLACEHOLDER} database-name placeholder.");
        }

        return new IntegrationTestDatabaseConfiguration(resolvedDatabaseNamePrefix, resolvedSqlServerConnectionStringTemplate);
    }

    public string CreateConnectionString(string databaseName) {
        if (string.IsNullOrWhiteSpace(databaseName)) {
            throw new InvalidOperationException("The integration-test database name cannot be empty.");
        }

        string result = string.Format(SqlServerConnectionStringTemplate, databaseName);
        return result;
    }

    public string CreateDatabaseName() {
        string result = $"{DatabaseNamePrefix}{Guid.NewGuid():N}";
        return result;
    }
}
