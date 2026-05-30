namespace FoodTour.Test.Controllers;

public abstract class AdminControllerTestBase : TestBase {
    private const string BASE_URL = "http://localhost";

    protected static AdminController CreateController(
        IAdminAccessService? adminAccessService = null,
        bool googleConfigured = true) {
        var controller = TestHelper.CreateAdminController(
            adminAccessService,
            TestHelper.CreateAdminConfiguration(googleConfigured));
        controller.ControllerContext = new ControllerContext {
            HttpContext = CreateHttpContext()
        };
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.Url = CreateUrlHelper();
        return controller;
    }

    protected static AdminController CreateAuthenticatedController(
        string email = TestHelper.ADMIN_EMAIL,
        bool googleConfigured = true) {
        var controller = CreateController(googleConfigured: googleConfigured);
        controller.ControllerContext.HttpContext.User = CreateAuthenticatedUser(email);
        return controller;
    }

    private static DefaultHttpContext CreateHttpContext() {
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock
            .Setup(s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(authServiceMock.Object);

        return new DefaultHttpContext {
            RequestServices = services.BuildServiceProvider()
        };
    }

    private static IUrlHelper CreateUrlHelper() {
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(ctx => $"{BASE_URL}/Admin/{ctx.Action}");
        urlHelperMock
            .Setup(u => u.IsLocalUrl(It.IsAny<string>()))
            .Returns<string>(url => url?.StartsWith('/') == true);
        return urlHelperMock.Object;
    }

    protected static ClaimsPrincipal CreateAuthenticatedUser(string email = TestHelper.ADMIN_EMAIL) {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, email)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
