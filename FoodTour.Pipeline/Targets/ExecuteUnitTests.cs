namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {
    public Target ExecuteUnitTests => _ => _
        .DependsOn(BuildSolution)
        .Executes(() => {
            var testProjects = Solution.GetAllProjects("*.Test");

            if (testProjects.Any()) {
                foreach (var project in testProjects) {
                    DotNetTasks.DotNetTest(s => s
                        .SetProjectFile(project)
                        .SetConfiguration(Configuration)
                        .SetNoBuild(true)
                    );
                }
            } else {
                Log.Warning("No test projects found in the solution.");
            }
        });
}
