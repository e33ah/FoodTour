namespace FoodTour.Test.Controllers;

public class AdminControllerLoginTests : AdminControllerTestBase {
    private const string DASHBOARD_URL = "/Admin/Dashboard";

    [Fact]
    public void Login_ReturnsView_WhenNotAuthenticated() {
        var sut = CreateController();

        var result = sut.Login();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void Login_SetsReturnUrl_InViewData() {
        var sut = CreateController();

        var result = (ViewResult)sut.Login(returnUrl: DASHBOARD_URL);

        result.ViewData["ReturnUrl"].ShouldBe(DASHBOARD_URL);
    }

    [Fact]
    public void Login_SetsGoogleConfiguredTrue_WhenCredentialsPresent() {
        var sut = CreateController(googleConfigured: true);

        var result = (ViewResult)sut.Login();

        result.ViewData["GoogleConfigured"].ShouldBe(true);
    }

    [Fact]
    public void Login_SetsGoogleConfiguredFalse_WhenCredentialsMissing() {
        var sut = CreateController(googleConfigured: false);

        var result = (ViewResult)sut.Login();

        result.ViewData["GoogleConfigured"].ShouldBe(false);
    }

    [Fact]
    public void Login_RedirectsToDashboard_WhenAlreadyAuthenticated() {
        var sut = CreateAuthenticatedController();

        var result = sut.Login();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.Dashboard));
    }

    [Fact]
    public void LoginWithGoogle_ReturnsChallengeResult_WhenGoogleConfigured() {
        var sut = CreateController(googleConfigured: true);

        var result = sut.LoginWithGoogle();

        result.ShouldBeOfType<ChallengeResult>();
    }

    [Fact]
    public void LoginWithGoogle_RedirectsToLogin_WhenGoogleNotConfigured() {
        var sut = CreateController(googleConfigured: false);

        var result = sut.LoginWithGoogle();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.Login));
    }

    [Fact]
    public void LoginWithGoogle_PassesReturnUrl_InChallengeProperties() {
        var sut = CreateController(googleConfigured: true);

        var result = (ChallengeResult)sut.LoginWithGoogle(returnUrl: DASHBOARD_URL);

        result.Properties?.RedirectUri.ShouldNotBeNullOrEmpty();
    }
}
