namespace FoodTour.ITest.UI.Tests;

internal static class UiTestArtifactPathBuilder {
    private const string DEFAULT_ARTIFACT_DIRECTORY_NAME = "TestResults";
    private const string PROJECT_FILE_NAME = "FoodTour.ITest.UI.csproj";
    private const string SCREENSHOT_DIRECTORY_NAME = "Screenshots";
    private const string SCREENSHOT_FILE_EXTENSION = ".png";
    private const string UNKNOWN_NAME = "Unknown";

    public static string CreateScreenshotPath(string testClassName, string testName) {
        string safeTestClassName = SanitisePathSegment(testClassName);
        string safeTestName = SanitisePathSegment(testName);
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);

        return Path.Combine(
            GetArtifactRoot(),
            SCREENSHOT_DIRECTORY_NAME,
            $"{safeTestClassName}_{safeTestName}_{timestamp}{SCREENSHOT_FILE_EXTENSION}");
    }

    internal static string GetArtifactRoot(string baseDirectory) {
        DirectoryInfo? currentDirectory = new(baseDirectory);

        while (currentDirectory is not null) {
            if (File.Exists(Path.Combine(currentDirectory.FullName, PROJECT_FILE_NAME))) {
                return Path.Combine(currentDirectory.FullName, DEFAULT_ARTIFACT_DIRECTORY_NAME);
            }

            currentDirectory = currentDirectory.Parent;
        }

        return Path.Combine(baseDirectory, DEFAULT_ARTIFACT_DIRECTORY_NAME);
    }

    private static string GetArtifactRoot() {
        return GetArtifactRoot(AppContext.BaseDirectory);
    }

    internal static string SanitisePathSegment(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return UNKNOWN_NAME;
        }

        char[] invalidFileNameCharacters = Path.GetInvalidFileNameChars();
        char[] sanitisedCharacters = [.. value
            .Trim()
            .Select(character => invalidFileNameCharacters.Contains(character) ? '_' : character)];
        string result = new(sanitisedCharacters);

        return string.IsNullOrWhiteSpace(result) ? UNKNOWN_NAME : result;
    }
}
