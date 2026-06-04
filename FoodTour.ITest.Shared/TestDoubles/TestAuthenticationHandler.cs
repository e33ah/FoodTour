namespace FoodTour.ITest.Shared.TestDoubles;

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    public const string AUTHENTICATION_SCHEME = "TestScheme";
    public const string TEST_ADMIN_EMAIL = "admin@test.com";
    private const string TEST_AUTH_HEADER = "X-Test-Auth";
    private const string TEST_AUTH_COOKIE = "X-Test-Auth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        bool isAuthenticated = Request.Headers.ContainsKey(TEST_AUTH_HEADER) ||
                              Request.Cookies.ContainsKey(TEST_AUTH_COOKIE);

        if (!isAuthenticated) {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Claim[] claims = [
            new Claim(ClaimTypes.NameIdentifier, TEST_ADMIN_EMAIL),
            new Claim(ClaimTypes.Name, "Test Admin"),
            new Claim(ClaimTypes.Email, TEST_ADMIN_EMAIL)
        ];

        ClaimsIdentity identity = new(claims, AUTHENTICATION_SCHEME);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, AUTHENTICATION_SCHEME);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) {
        Response.StatusCode = 302;
        Response.Headers.Location = "/Admin/Login";
        return Task.CompletedTask;
    }
}
