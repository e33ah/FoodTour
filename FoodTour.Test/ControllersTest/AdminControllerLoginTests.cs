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

    [Fact]
    public async Task GoogleCallback_RedirectsToAccessDenied_WhenEmailIsMissing() {
        var sut = CreateController();
        sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await sut.GoogleCallback();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.AccessDenied));
    }

    [Fact]
    public async Task GoogleCallback_RedirectsToAccessDenied_WhenEmailIsNotAllowed() {
        var adminAccessService = Mock.Of<IAdminAccessService>(service => service.IsAllowed(It.IsAny<string>()) == false);
        var sut = CreateController(adminAccessService);
        sut.ControllerContext.HttpContext.User = CreateAuthenticatedUser("blocked@test.com");

        var result = await sut.GoogleCallback();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.AccessDenied));
    }

    [Fact]
    public async Task GoogleCallback_RedirectsToLocalReturnUrl_WhenEmailIsAllowed() {
        var sut = CreateController();
        sut.ControllerContext.HttpContext.User = CreateAuthenticatedUser();

        var result = await sut.GoogleCallback("/Admin/Visits");

        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldBe("/Admin/Visits");
    }

    [Fact]
    public async Task GoogleCallback_RedirectsToDashboard_WhenReturnUrlIsExternal() {
        var sut = CreateController();
        sut.ControllerContext.HttpContext.User = CreateAuthenticatedUser();

        var result = await sut.GoogleCallback("https://attacker.example");

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.Dashboard));
    }
}
