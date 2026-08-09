namespace FoodTour.Pipeline;

public partial class Build : FalloutBuild {
    public Target BuildSolution => _ => _
        .Executes(() => {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetDeterministic(IsServerBuild)
                .SetContinuousIntegrationBuild(IsServerBuild)
            );
        });
}