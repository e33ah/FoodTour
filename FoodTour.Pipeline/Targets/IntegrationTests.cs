namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {
    public Target IntegrationTests => _ => _
        .DependsOn(BuildSolution)
        .Executes(() => {
            var testProjects = Solution.GetAllProjects("*.ITest.*");

            if (testProjects.Any()) {
                foreach (var project in testProjects) {
                    DotNetTasks.DotNetRun(s => s
                        .SetProjectFile(project)
                        .SetConfiguration(Configuration)
                        .SetNoBuild(true)
                    );
                }
            } else {
                Log.Warning("No UI integration test projects found in the solution.");
            }
        });
}
