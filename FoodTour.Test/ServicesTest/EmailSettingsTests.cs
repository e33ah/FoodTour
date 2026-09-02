namespace FoodTour.Test.ServicesTest;

public class EmailSettingsTests {
    [Fact]
    public void EmailSettings_ShouldInitializeWithDefaultValues() {
        var emailSettings = new EmailSettings {
            Host = string.Empty,
            FromEmail = string.Empty,
            FromName = string.Empty
        };

        emailSettings.Port.ShouldBe(0);
        emailSettings.UseSsl.ShouldBeFalse();
        emailSettings.Username.ShouldBeEmpty();
        emailSettings.Password.ShouldBeEmpty();
    }
}