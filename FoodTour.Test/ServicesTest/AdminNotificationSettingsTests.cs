namespace FoodTour.Test.ServicesTest;

public class AdminNotificationSettingsTests {
    [Fact]
    public void AdminNotificationSettings_ShouldInitializeWithDefaultValues() {
        var adminNotificationSettings = new AdminNotificationSettings {
        };
        adminNotificationSettings.AdminEmail.ShouldBeEmpty();
        adminNotificationSettings.NotifyOnVisitRecorded.ShouldBeFalse();
    }
}
