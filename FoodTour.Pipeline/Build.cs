namespace FoodTour.Pipeline;

[GitHubActions("foodtour-ci", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest, GitHubActionsTrigger.WorkflowDispatch },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(ExecuteUnitTests) }, AutoGenerate = false)]
[GitHubActions("foodtour-cd", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.WorkflowDispatch },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(ExecuteUnitTests), nameof(Publish), nameof(Release) },
    ImportSecrets = new[] { nameof(FtpHost), nameof(FtpUser), nameof(FtpPassword), nameof(GoogleClientId), nameof(GoogleClientSecret), nameof(FareHarborWebhookKey) }, AutoGenerate = false)]
[GitHubActions("foodtour-itest", GitHubActionsImage.WindowsLatest, On = new[] { GitHubActionsTrigger.WorkflowDispatch, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(BuildSolution), nameof(ExecuteUnitTests), nameof(IntegrationTests) }, AutoGenerate = false)]
public partial class Build : FalloutBuild {
    public static int Main() {
        if (IsLocalBuild) {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Build>(optional: true)
                .Build();
            SetEnvironmentVariableIfMissing(nameof(FtpHost), config[nameof(FtpHost)]);
            SetEnvironmentVariableIfMissing(nameof(FtpUser), config[nameof(FtpUser)]);
            SetEnvironmentVariableIfMissing(nameof(FtpPassword), config[nameof(FtpPassword)]);
            SetEnvironmentVariableIfMissing(nameof(GoogleClientId), config[nameof(GoogleClientId)]);
            SetEnvironmentVariableIfMissing(nameof(GoogleClientSecret), config[nameof(GoogleClientSecret)]);
            SetEnvironmentVariableIfMissing(nameof(FareHarborWebhookKey), config[nameof(FareHarborWebhookKey)]);
        }
        return Execute<Build>(x => x.FoodTourCI);
    }

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath DependencyDirectory => RootDirectory / "_Dependencies/Publish_Support";
    AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";
    AbsolutePath TestSolution => RootDirectory / "FoodTour.ITest.slnx";
    AbsolutePath UiTestProjectDirectory => RootDirectory / "FoodTour.ITest.UI";

    public Target FoodTourCI => _ => _
        .DependsOn(ExecuteUnitTests);
    public Target FoodTourCD => _ => _
        .DependsOn(Release);

    public Target FoodTourIntegrationTest => _ => _
        .DependsOn(ExecuteUnitTests, IntegrationTests);

    static void SetEnvironmentVariableIfMissing(string name, string value) {
        if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))) {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
