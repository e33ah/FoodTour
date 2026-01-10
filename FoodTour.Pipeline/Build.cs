namespace FoodTour.Pipeline;


[GitHubActions("foodtour-ci", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(RunUnitTests) }, AutoGenerate = false)]
public partial class Build : Nuke.Common.NukeBuild {
    public static int Main() => Execute<Build>(x => x.FoodTourCI);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    public Target FoodTourCI => _ => _
        .DependsOn(RunUnitTests);
}
