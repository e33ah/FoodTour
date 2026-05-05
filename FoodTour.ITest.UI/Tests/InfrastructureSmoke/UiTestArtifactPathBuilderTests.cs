using FoodTour.ITest.UI.Tests;

namespace FoodTour.ITest.UI.InfrastructureSmoke;

public class UiTestArtifactPathBuilderTests {
    [Fact]
    public void CreateScreenshotPath_ReturnsWindowsSafeFileName_WhenNamesContainInvalidCharacters() {
        string result = UiTestArtifactPathBuilder.CreateScreenshotPath("Booking:Selection/Tests", "CreateTour?Failure");
        string fileName = Path.GetFileName(result);

        fileName.ShouldContain("Booking_Selection_Tests");
        fileName.ShouldContain("CreateTour_Failure");
        fileName.ShouldEndWith(".png");
    }

    [Fact]
    public void GetArtifactRoot_ReturnsProjectTestResultsPath_WhenProjectFileExistsInAncestorDirectory() {
        string tempProjectRootDirectory = Path.Combine(Path.GetTempPath(), $"FoodTour.ITest.UI-{Guid.NewGuid():N}");
        string baseDirectory = Path.Combine(tempProjectRootDirectory, "FoodTour.ITest.UI", "bin", "Debug", "net10.0");
        string projectDirectory = Path.Combine(tempProjectRootDirectory, "FoodTour.ITest.UI");
        string projectFilePath = Path.Combine(projectDirectory, "FoodTour.ITest.UI.csproj");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(baseDirectory);
        File.WriteAllText(projectFilePath, "<Project />");

        try {
            string result = UiTestArtifactPathBuilder.GetArtifactRoot(baseDirectory);

            result.ShouldBe(Path.Combine(projectDirectory, "TestResults"));
        } finally {
            Directory.Delete(tempProjectRootDirectory, true);
        }
    }

    [Fact]
    public void SanitisePathSegment_ReturnsUnknown_WhenValueIsWhitespace() {
        string result = UiTestArtifactPathBuilder.SanitisePathSegment("   ");

        result.ShouldBe("Unknown");
    }
}
