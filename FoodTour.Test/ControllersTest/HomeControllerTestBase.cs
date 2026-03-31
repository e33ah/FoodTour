namespace FoodTour.Test.Controllers;

public abstract class HomeControllerTestBase {
    private const string BASE_URL = "http://localhost/Home";
    protected static HomeController CreateController() {
        var context = TestHelper.CreateInMemoryContext();
        return CreateConfiguredController(context);
    }

    protected static HomeController CreateConfiguredController(FoodTourContext context) {
        var controller = TestHelper.CreateHomeController(context);

        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        return controller;
    }

    protected static (HomeController controller, Mock<IEmailSender> emailSenderMock) CreateConfiguredControllerWithEmailSenderMock(FoodTourContext context) {
        string urlPath = "FoodTourCode?qrToken=abc";
        var (controller, emailSenderMock) = TestHelper.CreateHomeControllerWithEmailSenderMock(context);
        var adminNotificationService = new AdminNotificationService(emailSenderMock.Object, TestHelper.CreateAdminNotificationOptions());
        var supplierNotificationService = new SupplierNotificationService(emailSenderMock.Object);

        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        string urlActionResult = $"{BASE_URL}/{urlPath}";
        controller.Url = Mock.Of<IUrlHelper>(u => u.Action(It.IsAny<UrlActionContext>()) == urlActionResult);
        controller.ControllerContext.HttpContext.Request.Scheme = "https";

        return (controller, emailSenderMock);
    }

    protected static HomeController CreateConfiguredControllerWithUrl(FoodTourContext context, string? urlPath) {
        string? fullUrl;
        if (string.IsNullOrEmpty(urlPath)) {
            fullUrl = urlPath;
        } else {
            urlPath = urlPath.StartsWith('/') ? urlPath : "/" + urlPath;
            fullUrl = $"{BASE_URL}{urlPath}";
        }
        var controller = CreateConfiguredController(context);
        controller.Url = Mock.Of<IUrlHelper>(u => u.Action(It.IsAny<UrlActionContext>()) == fullUrl);

        controller.ControllerContext.HttpContext.Request.Scheme = "https";
        return controller;
    }
}