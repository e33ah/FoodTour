namespace FoodTour.ITest.UI.Tests;

public class UiTestHostConfigurationTests {
    [Fact]
    public void Create_ReturnsConfiguredDatabaseAndEmailSettings_WhenConfigurationContainsUiTestValues() {
        Dictionary<string, string?> settings = new() {
            ["UiTests:Database:DatabaseNamePrefix"] = "CiUiTests_",
            ["UiTests:Database:SqlServerConnectionStringTemplate"] = "Server=sql.example;Database={0};User Id=test;Password=test;",
            ["UiTests:EmailSettings:Host"] = "smtp.test.local",
            ["UiTests:EmailSettings:Port"] = "2525",
            ["UiTests:EmailSettings:UseSsl"] = "true",
            ["UiTests:EmailSettings:FromEmail"] = "uitests@example.com",
            ["UiTests:EmailSettings:FromName"] = "UI Tests"
        };
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddInMemoryCollection(settings);
        var configuration = configurationBuilder.Build();

        var sut = UiTestHostConfiguration.Create(configuration);

        sut.Database.DatabaseNamePrefix.ShouldBe("CiUiTests_");
        sut.Database.SqlServerConnectionStringTemplate.ShouldBe("Server=sql.example;Database={0};User Id=test;Password=test;");
        sut.EmailSettings.Host.ShouldBe("smtp.test.local");
        sut.EmailSettings.Port.ShouldBe(2525);
        sut.EmailSettings.UseSsl.ShouldBeTrue();
        sut.EmailSettings.FromEmail.ShouldBe("uitests@example.com");
        sut.EmailSettings.FromName.ShouldBe("UI Tests");
    }
}
