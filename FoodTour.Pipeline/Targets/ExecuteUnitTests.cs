namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {
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
