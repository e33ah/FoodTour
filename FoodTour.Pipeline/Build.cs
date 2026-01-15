namespace FoodTour.Pipeline;

[GitHubActions("foodtour-ci", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(ExecuteUnitTests) }, AutoGenerate = false)]
[GitHubActions("foodtour-cd", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.WorkflowDispatch },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(ExecuteUnitTests), nameof(Publish), nameof(Release) }, 
    ImportSecrets = new[] { nameof(FtpHost), nameof(FtpUser), nameof(FtpPassword) }, AutoGenerate = false)]
public partial class Build : NukeBuild {
    public static int Main() => Execute<Build>(x => x.FoodTourCI);

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";
    AbsolutePath DependencyDirectory => RootDirectory / "_Dependencies/Publish_Support";

    public Target FoodTourCI => _ => _
        .DependsOn(ExecuteUnitTests);
}
