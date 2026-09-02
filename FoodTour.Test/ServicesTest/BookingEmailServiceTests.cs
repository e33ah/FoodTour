namespace FoodTour.Test.Services;

public class BookingEmailServiceTests {
    private const string FOOD_TOUR_CODE_URL = "https://example.test/Home/FoodTourCode?qrToken=abc";
    private const string USER_SUPPORT_URL = "https://example.test/Home/UserSupport";

    [Fact]
    public async Task GenerateBookingConfirmationEmailAsync_IncludesFoodTourCodeLinkAndSupplierDetails_WhenUrlProvided() {
        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => {
                var typedModel = model as FoodTour.Models.BookingConfirmationEmailModel;
                return $"<html style=\"font-family: Arial\">{typedModel!.FoodTourCodeUrl} {typedModel.UserSupportUrl} {string.Join(" ", typedModel.TastingExperiences.Select(e => $"{e.Name} {e.Address}"))} present your QR code</html>";
            });
        var sut = new BookingEmailService(templateRendererMock.Object);

        var booking = TestHelper.CreateValidBooking([1, 2]);

        var suppliers = new List<Supplier> {
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2)
        };


        var (subject, body) = await sut.GenerateBookingConfirmationEmailAsync(booking, suppliers, FOOD_TOUR_CODE_URL, USER_SUPPORT_URL);

        subject.ShouldContain("Booking Confirmation");
        body.ShouldContain("font-family: Arial");
        body.ShouldContain(FOOD_TOUR_CODE_URL);
        body.ShouldContain(USER_SUPPORT_URL);
        body.ShouldContain(TestHelper.SUPPLIER_NAME_1);
        body.ShouldContain(TestHelper.SUPPLIER_ADDRESS_1);
        body.ShouldContain("present your QR code");
    }

    [Fact]
    public async Task GenerateBookingConfirmationEmailAsync_IncludesNoSelectionsMessage_WhenNoSuppliersProvided() {
        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => {
                var typedModel = model as FoodTour.Models.BookingConfirmationEmailModel;
                return typedModel!.TastingExperiences.Count == 0
                    ? "<html>No tasting experiences selected</html>"
                    : "<html>Experiences listed</html>";
            });
        var sut = new BookingEmailService(templateRendererMock.Object);
        var booking = TestHelper.CreateValidBooking([]);
        var suppliers = Array.Empty<Supplier>();

        var (_, body) = await sut.GenerateBookingConfirmationEmailAsync(booking, suppliers, FOOD_TOUR_CODE_URL, USER_SUPPORT_URL);

        body.ShouldContain("No tasting experiences selected");
    }

    [Fact]
    public async Task GenerateBookingConfirmationEmailAsync_UsesCustomerEmailAndEmptyAddress_WhenCustomerNameIsWhitespaceAndSupplierAddressIsNull() {
        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        FoodTour.Models.BookingConfirmationEmailModel? observedModel = null;
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => {
                observedModel = (FoodTour.Models.BookingConfirmationEmailModel)model;
                return "<html>Rendered booking confirmation</html>";
            });
        var sut = new BookingEmailService(templateRendererMock.Object);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.CustomerEmail = "customer@example.com";
        booking.CustomerName = "   ";
        var supplier = TestHelper.CreateSupplier(1);
        supplier.Address = null;

        var (_, body) = await sut.GenerateBookingConfirmationEmailAsync(booking, [supplier], FOOD_TOUR_CODE_URL, USER_SUPPORT_URL);

        body.ShouldContain("Rendered booking confirmation");
        observedModel.ShouldNotBeNull();
        observedModel!.CustomerName.ShouldBe(WebUtility.HtmlEncode(booking.CustomerEmail));
        observedModel.TastingExperiences.Single().Address.ShouldBe(string.Empty);
        templateRendererMock.Verify(x => x.RenderTemplateAsync("Emails/BookingConfirmation", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GenerateBookingConfirmationEmailAsync_UsesCustomerNameAndEncodedAddress_WhenProvided() {
        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        FoodTour.Models.BookingConfirmationEmailModel? observedModel = null;
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => {
                observedModel = (FoodTour.Models.BookingConfirmationEmailModel)model;
                return "<html>Rendered customer name</html>";
            });
        var sut = new BookingEmailService(templateRendererMock.Object);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.CustomerName = "Jane Doe";
        var supplier = TestHelper.CreateSupplier(1);
        supplier.Address = "123 Test Street & 4th Ave";

        await sut.GenerateBookingConfirmationEmailAsync(booking, [supplier], FOOD_TOUR_CODE_URL, USER_SUPPORT_URL);

        observedModel.ShouldNotBeNull();
        observedModel!.CustomerName.ShouldBe(WebUtility.HtmlEncode(booking.CustomerName));
        observedModel.TastingExperiences.Single().Address.ShouldBe(WebUtility.HtmlEncode(supplier.Address));
        templateRendererMock.Verify(x => x.RenderTemplateAsync("Emails/BookingConfirmation", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GeneratePartnerBookingConfirmationEmailAsync_IncludesValidationAndPrivacyLinks() {
        const string PARTNER_VALIDATION_URL = "https://example.test/Home/CompletePartnerBookingConfirmation?validation=abc";
        const string PRIVACY_POLICY_URL = "https://example.test/Home/Privacy";
        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => {
                var typedModel = model as FoodTour.Models.PartnerBookingConfirmationEmailModel;
                return $"<html>{typedModel!.ValidationUrl} {typedModel.PrivacyPolicyUrl} {typedModel.BookingReference}</html>";
            });
        var sut = new BookingEmailService(templateRendererMock.Object);

        var (subject, body) = await sut.GeneratePartnerBookingConfirmationEmailAsync(TestHelper.VALID_EMAIL, TestHelper.VALID_VOUCHER, PARTNER_VALIDATION_URL, PRIVACY_POLICY_URL);

        subject.ShouldContain("Confirm your Weymouth Self-Guided Tasting Tour booking");
        body.ShouldContain(PARTNER_VALIDATION_URL);
        body.ShouldContain(PRIVACY_POLICY_URL);
        body.ShouldContain(TestHelper.VALID_VOUCHER);
    }
}
