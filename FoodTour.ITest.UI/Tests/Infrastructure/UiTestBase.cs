namespace FoodTour.ITest.UI.Tests;

public abstract class UiTestBase(FoodTourApplicationFixture fixture) {
    private const int MAX_DIAGNOSTIC_ENTRIES_PER_TYPE = 10;

    protected FoodTourApplicationFixture Fixture { get; } = fixture;

    protected Task ExecutePageTestAsync(Func<IPage, Task> test, [CallerMemberName] string testName = "") {
        return ExecutePageTestAsync((_, page) => test(page), testName);
    }

    protected async Task ExecutePageTestAsync(Func<IBrowserContext, IPage, Task> test, [CallerMemberName] string testName = "") {
        await using var browserContext = await Fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var failureDiagnostics = new UiTestFailureDiagnostics();

        page.Console += (_, message) => {
            if (!string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            failureDiagnostics.AddConsoleError($"{message.Type}: {message.Text}");
        };

        page.PageError += (_, message) => failureDiagnostics.AddPageError(message);
        page.RequestFailed += (_, request) => failureDiagnostics.AddRequestFailure($"{request.Method} {request.Url} {request.Failure}");

        try {
            await test(browserContext, page);
        } catch {
            string? screenshotPath = await TryCaptureScreenshotAsync(page, testName);
            await WriteFailureDiagnosticsAsync(page, testName, screenshotPath, failureDiagnostics);
            throw;
        }
    }

    private async Task WriteFailureDiagnosticsAsync(IPage page, string testName, string? screenshotPath, UiTestFailureDiagnostics failureDiagnostics) {
        WriteFailureLog($"UI test failed: {GetType().Name}.{testName}");
        WriteFailureLog($"Current URL: {page.Url}");
        WriteFailureLog($"Screenshot: {screenshotPath ?? "not captured"}");

        var alertMessages = await TryGetAlertMessagesAsync(page);
        foreach (string alertMessage in alertMessages.Take(MAX_DIAGNOSTIC_ENTRIES_PER_TYPE)) {
            WriteFailureLog($"Visible alert: {alertMessage}");
        }

        foreach (string consoleError in failureDiagnostics.ConsoleErrors.Take(MAX_DIAGNOSTIC_ENTRIES_PER_TYPE)) {
            WriteFailureLog($"Console error: {consoleError}");
        }

        foreach (string pageError in failureDiagnostics.PageErrors.Take(MAX_DIAGNOSTIC_ENTRIES_PER_TYPE)) {
            WriteFailureLog($"Page error: {pageError}");
        }

        foreach (string requestFailure in failureDiagnostics.RequestFailures.Take(MAX_DIAGNOSTIC_ENTRIES_PER_TYPE)) {
            WriteFailureLog($"Request failed: {requestFailure}");
        }
    }

    private static async Task<IReadOnlyList<string>> TryGetAlertMessagesAsync(IPage page) {
        try {
            var alertTexts = await page.Locator("[role='alert']").AllInnerTextsAsync();
            return [.. alertTexts
                .Select(alertText => alertText.Trim())
                .Where(alertText => !string.IsNullOrWhiteSpace(alertText))];
        } catch (Exception exception) {
            WriteFailureLog($"Unable to read alert text: {exception.Message}");
            return [];
        }
    }

    private async Task<string?> TryCaptureScreenshotAsync(IPage page, string testName) {
        string screenshotPath = UiTestArtifactPathBuilder.CreateScreenshotPath(GetType().Name, testName);

        try {
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath) ?? throw new InvalidOperationException("The screenshot path did not include a directory."));
            await page.ScreenshotAsync(new PageScreenshotOptions {
                FullPage = true,
                Path = screenshotPath
            });
            return screenshotPath;
        } catch (Exception exception) {
            WriteFailureLog($"Failed to capture screenshot to {screenshotPath}: {exception}");
            return null;
        }
    }

    private static void WriteFailureLog(string message) {
        TestContext.Current.TestOutputHelper?.WriteLine(message);
        Console.Error.WriteLine(message);
    }

    private sealed class UiTestFailureDiagnostics {
        public List<string> ConsoleErrors { get; } = [];

        public List<string> PageErrors { get; } = [];

        public List<string> RequestFailures { get; } = [];

        public void AddConsoleError(string message) {
            AddMessage(ConsoleErrors, message);
        }

        public void AddPageError(string message) {
            AddMessage(PageErrors, message);
        }

        public void AddRequestFailure(string message) {
            AddMessage(RequestFailures, message);
        }

        private static void AddMessage(List<string> entries, string message) {
            if (entries.Count >= MAX_DIAGNOSTIC_ENTRIES_PER_TYPE) {
                return;
            }

            entries.Add(message);
        }
    }
}
