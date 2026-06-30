namespace FoodTour.ITest.Shared.Configuration;

public sealed class IntegrationTestEmailConfiguration {
    private const string DEFAULT_FROM_EMAIL = "itest@localhost";
    private const string DEFAULT_HOST = "localhost";
    private const int DEFAULT_PORT = 25;
    private const bool DEFAULT_USE_SSL = false;

    private IntegrationTestEmailConfiguration(string host, int port, bool useSsl, string fromEmail, string fromName) {
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

    public static IntegrationTestEmailConfiguration Create(string defaultFromName, string? host = null, string? port = null, string? useSsl = null, string? fromEmail = null, string? fromName = null) {
        if (string.IsNullOrWhiteSpace(defaultFromName)) {
            throw new InvalidOperationException("The default integration-test email sender name cannot be empty.");
        }

        string resolvedHost = string.IsNullOrWhiteSpace(host) ? DEFAULT_HOST : host.Trim();
        string resolvedFromEmail = string.IsNullOrWhiteSpace(fromEmail) ? DEFAULT_FROM_EMAIL : fromEmail.Trim();
        string resolvedFromName = string.IsNullOrWhiteSpace(fromName) ? defaultFromName.Trim() : fromName.Trim();
        bool resolvedUseSsl = !string.IsNullOrWhiteSpace(useSsl) && bool.Parse(useSsl);
        int resolvedPort = string.IsNullOrWhiteSpace(port) ? DEFAULT_PORT : int.Parse(port);

        return new IntegrationTestEmailConfiguration(resolvedHost, resolvedPort, resolvedUseSsl, resolvedFromEmail, resolvedFromName);
    }
}
