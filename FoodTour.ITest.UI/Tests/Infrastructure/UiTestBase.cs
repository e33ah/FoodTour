namespace FoodTour.ITest.UI.Tests;

public abstract class UiTestBase(FoodTourApplicationFixture fixture) {
    protected FoodTourApplicationFixture Fixture { get; } = fixture;

    protected Task ExecutePageTestAsync(Func<IPage, Task> test, [CallerMemberName] string testName = "") {
        return ExecutePageTestAsync((_, page) => test(page), testName);
    }

    protected async Task ExecutePageTestAsync(Func<IBrowserContext, IPage, Task> test, [CallerMemberName] string testName = "") {
        await using var browserContext = await Fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();

        try {
            await test(browserContext, page);
        } catch {
            await TryCaptureScreenshotAsync(page, testName);
            throw;
        }
    }

    private async Task TryCaptureScreenshotAsync(IPage page, string testName) {
        string screenshotPath = UiTestArtifactPathBuilder.CreateScreenshotPath(GetType().Name, testName);

        try {
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath) ?? throw new InvalidOperationException("The screenshot path did not include a directory."));
            await page.ScreenshotAsync(new PageScreenshotOptions {
                FullPage = true,
                Path = screenshotPath
            });
            TestContext.Current.SendDiagnosticMessage("Saved failure screenshot to {0}", screenshotPath);
        } catch (Exception exception) {
            TestContext.Current.SendDiagnosticMessage("Failed to capture screenshot to {0}: {1}", screenshotPath, exception);
        }
    }
}
