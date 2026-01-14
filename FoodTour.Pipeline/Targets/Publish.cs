namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {
    public Target Publish => _ => _
        .DependsOn(ExecuteUnitTests)
        .Executes(() => {
            var project = Solution.Projects.First(x => x.Name == "FoodTour");

            DotNetTasks.DotNetPublish(s => s
                .SetProject(project)
                .SetConfiguration(Configuration.Release)
                .SetOutput(ArtifactsDirectory / "publish")
                .EnableNoRestore()
                .EnableNoBuild()
            );
        });
}