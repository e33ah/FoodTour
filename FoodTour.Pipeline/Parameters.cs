namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild && !IsLocalDeployEnabled ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    [Parameter("Enable local deploy")]
    static readonly bool IsLocalDeployEnabled = false;

    [Parameter("FTP host name")]
    [Secret]
    readonly string FtpHost;

    [Parameter("FTP username")]
    [Secret]
    readonly string FtpUser;

    [Parameter("FTP password")]
    [Secret]
    readonly string FtpPassword;

    [Parameter("FTP target directory")]
    readonly string FtpRemotePath = "/";
}
