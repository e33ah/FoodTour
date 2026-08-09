namespace FoodTour.Pipeline;

public partial class Build : FalloutBuild {
    public Target ExecuteUnitTests => _ => _
        .DependsOn(BuildSolution)
        .Executes(() => {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoBuild(true)
            );
        });
}
