namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {

    public Target Release => _ => _
        .OnlyWhenDynamic(() => (IsLocalDeployEnabled || IsServerBuild))
        .DependsOn(PatchWebConfig)
        .Requires(() => FtpHost)
        .Requires(() => FtpUser)
        .Requires(() => FtpPassword)
        .Executes(() => {
            const int DEFAULT_TIMEOUT_MS = 10_000;
            const int HEARTBEAT_INTERVAL = 10;
            const string AO_FILENAME = "app_offline.htm";
            var lastProgressLog = DateTime.MinValue;
            var appOfflinePath = DependencyDirectory / AO_FILENAME;

            void ReportHeartbeat(FtpProgress progress) {
                if (DateTime.UtcNow - lastProgressLog < TimeSpan.FromSeconds(HEARTBEAT_INTERVAL)) { return; }

                lastProgressLog = DateTime.UtcNow;

                Log.Information("FTP deploy still running...");
            }

            if (!Directory.Exists(PublishDirectory)) {
                Log.Error("Local publish folder not found: {PublishDirectory}. Did the Publish target run?", PublishDirectory);
                throw new InvalidOperationException("Publish output missing");
            }
            bool appOfflineExists = File.Exists(appOfflinePath);
            if (!appOfflineExists) {
                Log.Warning("{aoFile} not found in _Dependencies folder: {AppOffline}", AO_FILENAME, appOfflinePath);
            }

            using var client = new FtpClient(FtpHost, FtpUser, FtpPassword);

            client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
            client.Config.ConnectTimeout = DEFAULT_TIMEOUT_MS;
            client.Config.ReadTimeout = DEFAULT_TIMEOUT_MS;

            try {
                client.Connect();

                Log.Information("Connected to FTP Host");

                string remoteDir = string.IsNullOrWhiteSpace(FtpRemotePath) ? "/" : FtpRemotePath;
                if (!remoteDir.EndsWith('/')) { remoteDir += "/"; }

                if (appOfflineExists) {
                    Log.Information("Uploading {aoFile} to {RemotePath}", AO_FILENAME, remoteDir);
                    var aoFileResult = client.UploadFile(
                        localPath: appOfflinePath,
                        remotePath: remoteDir + AO_FILENAME,
                        existsMode: FtpRemoteExists.Overwrite,
                        createRemoteDir: true
                        );
                    if (!aoFileResult.IsSuccess()) {
                        Log.Error("Failed to upload {aoFile} to {RemotePath}", AO_FILENAME, remoteDir);
                        throw new Exception("Failed to upload app_offline.htm");
                    }
                    Log.Information("{aoFile} uploaded.", AO_FILENAME);
                } else {
                    Log.Warning("Skipping upload of {aoFile} as it does not exist.", AO_FILENAME);
                }

                var dirResult = client.UploadDirectory(
                localFolder: PublishDirectory,
                remoteFolder: remoteDir,
                mode: FtpFolderSyncMode.Update,
                existsMode: FtpRemoteExists.Overwrite,
                verifyOptions: FtpVerify.Retry,
                progress: ReportHeartbeat
                );

                Log.Information("FTP deploy to {RemotePath} completed: {Uploaded} uploaded, {Skipped} skipped, {Failed} failed",
                    FtpRemotePath,
                    dirResult.Count(r => r.IsSuccess),
                    dirResult.Count(r => r.IsSkipped),
                    dirResult.Count(r => r.IsFailed)
                    );

                if (dirResult.Any(r => r.IsFailed)) {
                    Log.Error("{Failed} files failed to upload.", dirResult.Count(r => r.IsFailed));
                    throw new Exception("FTP upload failures detected.");
                }

                if (appOfflineExists) {
                    Log.Information("Removing {aoFile} from {RemotePath}", AO_FILENAME, remoteDir);
                    client.DeleteFile(remoteDir + AO_FILENAME);
                    Log.Information("{aoFile} removed.", AO_FILENAME);
                }

            } catch (Exception ex) {
                Log.Error(ex, "FTP deploy failed");
                throw;

            } finally {
                if (client.IsConnected) {
                    client.Disconnect();
                    Log.Information("FTP Host connection closed.");
                }
            }
        });
}