namespace FoodTour.Test.Services;

public class AdminAccessServiceTests : TestBase {
    [Fact]
    public void IsAllowed_ReturnsTrue_WhenEmailIsInWhitelist() {
        var sut = new AdminAccessService(TestHelper.CreateAdminSettingsOptions(TestHelper.ADMIN_EMAIL));

        bool result = sut.IsAllowed(TestHelper.ADMIN_EMAIL);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAllowed_ReturnsTrue_WhenEmailMatchesCaseInsensitively() {
        var sut = new AdminAccessService(TestHelper.CreateAdminSettingsOptions(TestHelper.ADMIN_EMAIL));

        bool result = sut.IsAllowed(TestHelper.ADMIN_EMAIL.ToUpperInvariant());

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAllowed_ReturnsFalse_WhenEmailIsNotInWhitelist() {
        var sut = new AdminAccessService(TestHelper.CreateAdminSettingsOptions(TestHelper.ADMIN_EMAIL));

        bool result = sut.IsAllowed("notallowed@test.com");

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAllowed_ReturnsFalse_WhenEmailIsEmpty() {
        var sut = new AdminAccessService(TestHelper.CreateAdminSettingsOptions(TestHelper.ADMIN_EMAIL));

        bool result = sut.IsAllowed(string.Empty);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAllowed_ReturnsFalse_WhenEmailIsWhitespace() {
        var sut = new AdminAccessService(TestHelper.CreateAdminSettingsOptions(TestHelper.ADMIN_EMAIL));

        bool result = sut.IsAllowed("   ");

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAllowed_ReturnsFalse_WhenWhitelistIsEmpty() {
        var sut = new AdminAccessService(TestHelper.CreateAdminSettingsOptions());

        bool result = sut.IsAllowed(TestHelper.ADMIN_EMAIL);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAllowed_ReturnsTrue_WhenOneOfMultipleAllowedEmailsMatches() {
        var sut = new AdminAccessService(TestHelper.CreateAdminSettingsOptions("other@test.com", TestHelper.ADMIN_EMAIL));

        bool result = sut.IsAllowed(TestHelper.ADMIN_EMAIL);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAllowed_ReturnsFalse_WhenNoEmailsMatch_WithMultipleInWhitelist() {
        var sut = new AdminAccessService(TestHelper.CreateAdminSettingsOptions("one@test.com", "two@test.com"));

        bool result = sut.IsAllowed(TestHelper.ADMIN_EMAIL);

        result.ShouldBeFalse();
    }
}
