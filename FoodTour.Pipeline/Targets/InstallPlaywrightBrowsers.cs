namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {
    private const string UI_ITEST_PROJECT_NAME = "FoodTour.ITest.UI";

    public Target InstallPlaywrightBrowsers => _ => _
        .DependsOn(BuildSolution)
        .OnlyWhenStatic(() => Solution.AllProjects.Any(project => string.Equals(project.Name, UI_ITEST_PROJECT_NAME, StringComparison.Ordinal)))
        .Executes(() => {
            var uiTestProject = Solution.AllProjects.First(project => string.Equals(project.Name, UI_ITEST_PROJECT_NAME, StringComparison.Ordinal));
            string installerRoot = (uiTestProject.Directory / "bin" / Configuration.ToString()).ToString();
            string installerScriptPath = Directory
                .EnumerateFiles(installerRoot, "playwright.ps1", SearchOption.AllDirectories)
                .SingleOrDefault()
                ?? throw new InvalidOperationException($"Could not find the Playwright installer script under '{installerRoot}'. Build the UI integration test project before installing Playwright.");

            ProcessTasks.StartProcess(
                    "pwsh",
                    $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{installerScriptPath}\" install chromium",
                    workingDirectory: uiTestProject.Directory)
                .AssertZeroExitCode();
        });
}
