namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {
    public Target BuildTestSolution => _ => _
        .Executes(() => {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(TestSolution)
                .SetConfiguration(Configuration)
                .SetDeterministic(IsServerBuild)
                .SetContinuousIntegrationBuild(IsServerBuild)
            );
        });
}