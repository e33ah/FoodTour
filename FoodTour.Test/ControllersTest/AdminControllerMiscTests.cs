namespace FoodTour.Test.Controllers;

public class AdminControllerMiscTests : AdminControllerTestBase {
    [Fact]
    public async Task Dashboard_ReturnsView() {
        var sut = CreateController();

        var result = await sut.Dashboard();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void AccessDenied_ReturnsView() {
        var sut = CreateController();

        var result = sut.AccessDenied();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void AccessDenied_ReturnsView_WhenAuthenticated() {
        // Regression: authenticated users whose email is not whitelisted must see the
        // AccessDenied view, not be redirected away. The "Back to Login" button posts
        // to Logout (not Login directly) to ensure sign-out before returning to Login.
        var sut = CreateAuthenticatedController();

        var result = sut.AccessDenied();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task Logout_RedirectsToLogin_WhenNotAuthenticated() {
        // Regression: AccessDenied "Back to Login" posts to Logout even when the user
        // has already been signed out. Logout must be [AllowAnonymous] for this to work.
        var sut = CreateController();

        var result = await sut.Logout();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.Login));
    }

    [Fact]
    public async Task Logout_RedirectsToLogin() {
        var sut = CreateAuthenticatedController();

        var result = await sut.Logout();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.Login));
    }
}
