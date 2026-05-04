namespace FoodTour.ITest.UI.Tests;

public class UiTestDatabaseConfigurationTests {
    [Fact]
    public void Create_ReturnsLocalDbDefaults_WhenOverridesAreNotProvided() {
        var sut = UiTestDatabaseConfiguration.Create();

        string databaseName = sut.CreateDatabaseName();
        string result = sut.CreateConnectionString("FoodTourITest_Sample");

        databaseName.ShouldStartWith("FoodTourITests_");
        result.ShouldContain("Data Source=(LocalDb)\\MSSQLLocalDB");
        result.ShouldContain("Initial Catalog=FoodTourITest_Sample");
    }

    [Fact]
    public void Create_ReturnsConfiguredOverrides_WhenOverridesAreProvided() {
        var sut = UiTestDatabaseConfiguration.Create(
            databaseNamePrefix: "CiUiTests_",
            sqlServerConnectionStringTemplate: "Server=sql.example;Database={0};User Id=test;Password=test;");

        string databaseName = sut.CreateDatabaseName();
        string result = sut.CreateConnectionString("FoodTourITest_Ci");

        databaseName.ShouldStartWith("CiUiTests_");
        result.ShouldBe("Server=sql.example;Database=FoodTourITest_Ci;User Id=test;Password=test;");
    }

    [Fact]
    public void Create_ThrowsInvalidOperationException_WhenDatabasePlaceholderIsMissing() {
        var sut = Should.Throw<InvalidOperationException>(() => UiTestDatabaseConfiguration.Create(
            sqlServerConnectionStringTemplate: "Server=sql.example;Database=FoodTourTests;User Id=test;Password=test;"));

        sut.Message.ShouldContain("{0}");
    }
}
