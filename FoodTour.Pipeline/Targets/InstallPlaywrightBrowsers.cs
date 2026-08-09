namespace FoodTour.Pipeline;

public partial class Build : FalloutBuild {
    public Target InstallPlaywrightBrowsers => _ => _
        .DependsOn(BuildTestSolution)
        .Executes(() => {
            string installerRoot = (UiTestProjectDirectory / "bin" / Configuration.ToString()).ToString();
            var installerScriptPath = (UiTestProjectDirectory
                / "bin"
                / Configuration.ToString())
                .GlobFiles("**/playwright.ps1")
                .SingleOrDefault()
                ?? throw new InvalidOperationException($"Could not find the Playwright installer script under '{installerRoot}'. Build the UI integration test project before installing Playwright.");

            ProcessTasks.StartProcess(
                    "pwsh",
                    $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{installerScriptPath}\" install chromium",
                    workingDirectory: UiTestProjectDirectory)
                .AssertZeroExitCode();
        });
}
