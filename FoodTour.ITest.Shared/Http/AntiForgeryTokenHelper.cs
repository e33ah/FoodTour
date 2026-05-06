namespace FoodTour.ITest.Shared.Http;

public static partial class AntiForgeryTokenHelper {
    private const string REQUEST_TOKEN_INPUT_NAME = "__RequestVerificationToken";
    private const string REQUEST_TOKEN_PATTERN = """<input[^>]*name="__RequestVerificationToken"[^>]*value="([^"]+)"[^>]*>""";

    [GeneratedRegex(REQUEST_TOKEN_PATTERN, RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RequestTokenRegex();

    public static async Task<AntiForgeryRequestData> GetRequestDataAsync(HttpClient client, string path, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        string html = await response.Content.ReadAsStringAsync(cancellationToken);
        var match = RequestTokenRegex().Match(html);
        if (!match.Success) {
            throw new InvalidOperationException($"Could not find the {REQUEST_TOKEN_INPUT_NAME} input in the response HTML.");
        }

        string requestToken = WebUtility.HtmlDecode(match.Groups[1].Value);
        string cookieHeader = CreateCookieHeader(response.Headers);
        if (string.IsNullOrWhiteSpace(cookieHeader)) {
            throw new InvalidOperationException("Could not find any anti-forgery cookies in the response headers.");
        }

        AntiForgeryRequestData result = new(requestToken, cookieHeader);
        return result;
    }

    private static string CreateCookieHeader(HttpResponseHeaders headers) {
        if (!headers.TryGetValues("Set-Cookie", out var values)) {
            return string.Empty;
        }

        string result = string.Join("; ", values
            .Select(static value => value.Split(';', 2)[0].Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value)));

        return result;
    }
}
