namespace FoodTour.Test.Controllers;

public abstract class HomeControllerTestBase : TestBase {
    private const string BASE_URL = "http://localhost";

    protected static HomeController CreateController() {
        var context = TestHelper.CreateInMemoryContext();
        return CreateConfiguredController(context);
    }

    protected static HomeController CreateConfiguredController(FoodTourContext context) {
        var controller = TestHelper.CreateHomeController(context);

        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.Url = CreateUrlHelper();
        controller.ControllerContext.HttpContext.Request.Scheme = "https";

        return controller;
    }

    protected static (HomeController controller, Mock<IEmailSender> emailSenderMock) CreateConfiguredControllerWithEmailSenderMock(FoodTourContext context) {
        var (controller, emailSenderMock) = TestHelper.CreateHomeControllerWithEmailSenderMock(context);

        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.Url = CreateUrlHelper();
        controller.ControllerContext.HttpContext.Request.Scheme = "https";

        return (controller, emailSenderMock);
    }

    protected static HomeController CreateConfiguredControllerWithUrl(FoodTourContext context, string? urlPath) {
        string? fullUrl;
        if (string.IsNullOrEmpty(urlPath)) {
            fullUrl = urlPath;
        } else {
            urlPath = urlPath.StartsWith('/') ? urlPath : "/" + urlPath;
            fullUrl = $"{BASE_URL}/Home{urlPath}";
        }
        var controller = CreateConfiguredController(context);
        controller.Url = Mock.Of<IUrlHelper>(u => u.Action(It.IsAny<UrlActionContext>()) == fullUrl);

        controller.ControllerContext.HttpContext.Request.Scheme = "https";
        return controller;
    }

    private static IUrlHelper CreateUrlHelper() {
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(helper => helper.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(actionContext => {
                string controllerName = actionContext.Controller ?? "Home";
                string actionName = actionContext.Action ?? "MockAction";
                var routeValues = new Microsoft.AspNetCore.Routing.RouteValueDictionary(actionContext.Values);
                if (routeValues.Count == 0) {
                    return $"{BASE_URL}/{controllerName}/{actionName}";
                }

                string queryString = string.Join("&", routeValues.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value?.ToString() ?? string.Empty)}"));
                return $"{BASE_URL}/{controllerName}/{actionName}?{queryString}";
            });
        return urlHelperMock.Object;
    }
}
