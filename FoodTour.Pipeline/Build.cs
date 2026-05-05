namespace FoodTour.Pipeline;

[GitHubActions("foodtour-ci", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(ExecuteUnitTests) }, AutoGenerate = false)]
[GitHubActions("foodtour-cd", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.WorkflowDispatch },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(ExecuteUnitTests), nameof(Publish), nameof(Release) },
    ImportSecrets = new[] { nameof(FtpHost), nameof(FtpUser), nameof(FtpPassword) }, AutoGenerate = false)]
[GitHubActions("foodtour-ui", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.WorkflowDispatch, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(ExecuteUnitTests), nameof(IntegrationTests) }, AutoGenerate = false)]
public partial class Build : NukeBuild {
    public static int Main() {
        if (IsLocalBuild) {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Build>(optional: true)
                .Build();
            SetEnvironmentVariableIfMissing(nameof(FtpHost), config[nameof(FtpHost)]);
            SetEnvironmentVariableIfMissing(nameof(FtpUser), config[nameof(FtpUser)]);
            SetEnvironmentVariableIfMissing(nameof(FtpPassword), config[nameof(FtpPassword)]);
        }
        return Execute<Build>(x => x.FoodTourCI);
    }

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";
    AbsolutePath DependencyDirectory => RootDirectory / "_Dependencies/Publish_Support";

    public Target FoodTourCI => _ => _
        .DependsOn(ExecuteUnitTests);

    public Target FoodTourUI => _ => _
        .DependsOn(ExecuteUnitTests, IntegrationTests);

    static void SetEnvironmentVariableIfMissing(string name, string value) {
        if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))) {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
