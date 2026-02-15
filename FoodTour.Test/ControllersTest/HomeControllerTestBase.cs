namespace FoodTour.Test.Controllers;

public abstract class HomeControllerTestBase {
    private const string BASE_URL = "http://localhost/Home";
    protected static HomeController CreateController() {
        var context = TestHelper.CreateInMemoryContext();
        return CreateConfiguredController(context);
    }

    protected static HomeController CreateConfiguredController(FoodTourContext context) {
        var emailSenderMock = CreateMockEmailSender();
        var emailSender = emailSenderMock.Object;
        var bookingEmailService = new BookingEmailService();

        return new HomeController(context, emailSender, bookingEmailService) {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    protected static (HomeController controller, Mock<IEmailSender> emailSenderMock) CreateConfiguredControllerWithEmailSenderMock(FoodTourContext context) {
        string urlPath = "FoodTourCode?qrToken=abc";
        var emailSenderMock = CreateMockEmailSender();
        var emailSender = emailSenderMock.Object;
        var bookingEmailService = new BookingEmailService();

        var controller = new HomeController(context, emailSender, bookingEmailService) {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

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

    private static Mock<IEmailSender> CreateMockEmailSender() {
        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return emailSenderMock;
    }
}