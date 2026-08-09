namespace FoodTour.Pipeline;

public partial class Build : FalloutBuild {
    public Target IntegrationTests => _ => _
        .DependsOn(InstallPlaywrightBrowsers)
        .Executes(() => {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(TestSolution)
                .SetConfiguration(Configuration)
                .SetNoBuild(true) // because BuildTestSolution already ran
            );
        });
}
