namespace FoodTour.Test.Controllers;

public class HomeControllerPartnerBookingConfirmationTests : HomeControllerTestBase {
    [Fact]
    public void PartnerBookingConfirmation_ReturnsViewWithPrefilledValues() {
        var sut = CreateController();

        var result = sut.PartnerBookingConfirmation(TestHelper.VALID_VOUCHER, TestHelper.VALID_EMAIL);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PartnerBookingConfirmationVM>();
        model.BookingReference.ShouldBe(TestHelper.VALID_VOUCHER);
        model.CustomerEmail.ShouldBe(TestHelper.VALID_EMAIL);
    }

    [Theory]
    [InlineData("expired", "This link has expired, please request a new one.")]
    [InlineData("invalid", "This confirmation link is invalid. Please request a new one.")]
    public void PartnerBookingConfirmation_SetsLinkErrorMessage_WhenLinkErrorProvided(string linkError, string expectedMessage) {
        var sut = CreateController();

        var result = sut.PartnerBookingConfirmation(TestHelper.VALID_VOUCHER, TestHelper.VALID_EMAIL, linkError);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PartnerBookingConfirmationVM>();
        model.LinkErrorMessage.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task PartnerBookingConfirmation_SendsValidationEmail_WhenBookingRequiresConfirmation() {
        var context = TestHelper.CreateInMemoryContext();
        context.Bookings.Add(new Booking {
            CustomerEmail = "pseudo@partner.test",
            PurchaseCode = TestHelper.VALID_CODE,
            VoucherNumber = TestHelper.VALID_VOUCHER,
            TourDate = TestHelper.validTourDate,
            RequiresEmailConfirmation = true
        });
        await context.SaveChangesAsync(CancellationToken);
        var emailSenderMock = TestHelper.CreateMockEmailSender();
        var sut = CreatePartnerBookingController(context, emailSenderMock, out _);

        var result = await sut.PartnerBookingConfirmation(new PartnerBookingConfirmationVM {
            BookingReference = TestHelper.VALID_VOUCHER,
            CustomerEmail = TestHelper.VALID_EMAIL
        });

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PartnerBookingConfirmationVM>();
        model.EmailSent.ShouldBeTrue();
        emailSenderMock.Verify(sender => sender.SendEmailAsync(
            TestHelper.VALID_EMAIL,
            It.IsAny<string>(),
            It.Is<string>(body => body.Contains("validation="))), Times.Once);
    }

    [Fact]
    public async Task PartnerBookingConfirmation_AddsModelError_WhenBookingIsNotPendingConfirmation() {
        var context = TestHelper.CreateInMemoryContext();
        context.Bookings.Add(new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            VoucherNumber = TestHelper.VALID_VOUCHER,
            TourDate = TestHelper.validTourDate,
            RequiresEmailConfirmation = false
        });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateConfiguredController(context);

        var result = await sut.PartnerBookingConfirmation(new PartnerBookingConfirmationVM {
            BookingReference = TestHelper.VALID_VOUCHER,
            CustomerEmail = TestHelper.VALID_EMAIL
        });

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CompletePartnerBookingConfirmation_UpdatesBookingAndRedirectsToIndex_WhenValidationIsValid() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "pseudo@partner.test",
            PurchaseCode = TestHelper.VALID_CODE,
            VoucherNumber = TestHelper.VALID_VOUCHER,
            TourDate = TestHelper.validTourDate,
            RequiresEmailConfirmation = true,
            QrToken = Guid.NewGuid()
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var emailSenderMock = TestHelper.CreateMockEmailSender();
        var sut = CreatePartnerBookingController(context, emailSenderMock, out var capturedEmailBody);

        await sut.PartnerBookingConfirmation(new PartnerBookingConfirmationVM {
            BookingReference = TestHelper.VALID_VOUCHER,
            CustomerEmail = TestHelper.VALID_EMAIL
        });

        string validation = GetValidationToken(capturedEmailBody());

        var result = await sut.CompletePartnerBookingConfirmation(validation);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
        redirect.RouteValues!["qrToken"].ShouldBe(booking.QrToken);
        redirect.RouteValues["bookingReference"].ShouldBe(TestHelper.VALID_VOUCHER);

        var updatedBooking = await context.Bookings.FindAsync([booking.Id], CancellationToken);
        updatedBooking.ShouldNotBeNull();
        updatedBooking.CustomerEmail.ShouldBe(TestHelper.VALID_EMAIL);
        updatedBooking.RequiresEmailConfirmation.ShouldBeFalse();
    }

    [Fact]
    public async Task CompletePartnerBookingConfirmation_RedirectsToPartnerBookingConfirmation_WhenValidationIsExpired() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "pseudo@partner.test",
            PurchaseCode = TestHelper.VALID_CODE,
            VoucherNumber = TestHelper.VALID_VOUCHER,
            TourDate = TestHelper.validTourDate,
            RequiresEmailConfirmation = true
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var partnerBookingConfirmationService = TestHelper.CreatePartnerBookingConfirmationService(context);
        string expiredValidation = partnerBookingConfirmationService.CreateValidationToken(booking.Id, TestHelper.VALID_EMAIL, TestHelper.VALID_VOUCHER, DateTime.UtcNow.AddMinutes(-1));
        var sut = CreateConfiguredController(context);

        var result = await sut.CompletePartnerBookingConfirmation(expiredValidation);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.PartnerBookingConfirmation));
        redirect.RouteValues!["bookingReference"].ShouldBe(TestHelper.VALID_VOUCHER);
        redirect.RouteValues["customerEmail"].ShouldBe(TestHelper.VALID_EMAIL);
        redirect.RouteValues["linkError"].ShouldBe("expired");
    }

    [Fact]
    public async Task CompletePartnerBookingConfirmation_RedirectsToPartnerBookingConfirmation_WhenValidationIsInvalid() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());

        var result = await sut.CompletePartnerBookingConfirmation("invalid-token");

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.PartnerBookingConfirmation));
        redirect.RouteValues!["linkError"].ShouldBe("invalid");
    }

    private static HomeController CreatePartnerBookingController(FoodTourContext context, Mock<IEmailSender> emailSenderMock, out Func<string> capturedEmailBody) {
        string emailBody = string.Empty;
        capturedEmailBody = () => emailBody;

        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        templateRendererMock
            .Setup(renderer => renderer.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => path switch {
                "Emails/PartnerBookingConfirmation" => ((PartnerBookingConfirmationEmailModel)model).ValidationUrl,
                _ => "<html>Mock email body</html>"
            });

        emailSenderMock
            .Setup(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, _, body) => emailBody = body)
            .Returns(Task.CompletedTask);

        var bookingEmailService = new BookingEmailService(templateRendererMock.Object);
        var partnerBookingConfirmationService = TestHelper.CreatePartnerBookingConfirmationService(context);
        var sut = new HomeController(
            context,
            emailSenderMock.Object,
            bookingEmailService,
            TestHelper.CreateSupportOptions(),
            new BookingValidationService(context),
            new BookingUpdateService(context),
            partnerBookingConfirmationService);

        sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        sut.ControllerContext.HttpContext.Request.Scheme = "https";

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(helper => helper.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(actionContext => {
                string controllerName = actionContext.Controller ?? "Home";
                string actionName = actionContext.Action ?? "MockAction";
                var routeValues = new Microsoft.AspNetCore.Routing.RouteValueDictionary(actionContext.Values);
                if (routeValues.Count == 0) {
                    return $"http://localhost/{controllerName}/{actionName}";
                }

                string queryString = string.Join("&", routeValues.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value?.ToString() ?? string.Empty)}"));
                return $"http://localhost/{controllerName}/{actionName}?{queryString}";
            });
        sut.Url = urlHelperMock.Object;

        return sut;
    }

    private static string GetValidationToken(string validationUrl) {
        var uri = new Uri(validationUrl);
        string query = uri.Query.TrimStart('?');
        string parameter = query.Split('&').Single(part => part.StartsWith("validation=", StringComparison.Ordinal));
        return Uri.UnescapeDataString(parameter["validation=".Length..]);
    }
}
