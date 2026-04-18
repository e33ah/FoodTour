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
}
