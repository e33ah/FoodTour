namespace FoodTour.Test.ServicesTest;

public class BookingEmailServiceTests {
    private const string FOOD_TOUR_CODE_URL = "https://example.test/Home/FoodTourCode?qrToken=abc";
    private const string USER_SUPPORT_URL = "https://example.test/Home/UserSupport";

    [Fact]
    public void GenerateBookingConfirmationEmail_IncludesFoodTourCodeLinkAndSupplierDetails_WhenUrlProvided() {
        var sut = new BookingEmailService();

        var booking = TestHelper.CreateValidBooking([1, 2]);

        var suppliers = new List<Supplier> {
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2)
        };


        var (subject, body) = sut.GenerateBookingConfirmationEmail(booking, suppliers, FOOD_TOUR_CODE_URL, USER_SUPPORT_URL);

        subject.ShouldContain("Booking Confirmation");
        body.ShouldContain("font-family: Arial");
        body.ShouldContain(FOOD_TOUR_CODE_URL);
        body.ShouldContain(USER_SUPPORT_URL);
        body.ShouldContain(TestHelper.SUPPLIER_NAME_1);
        body.ShouldContain(TestHelper.SUPPLIER_ADDRESS_1);
        body.ShouldContain("present your QR code");
    }

    [Fact]
    public void GenerateBookingConfirmationEmail_IncludesNoSelectionsMessage_WhenNoSuppliersProvided() {
        var sut = new BookingEmailService();
        var booking = TestHelper.CreateValidBooking([]);
        var suppliers = Array.Empty<Supplier>();

        var (_, body) = sut.GenerateBookingConfirmationEmail(booking, suppliers, FOOD_TOUR_CODE_URL, USER_SUPPORT_URL);

        body.ShouldContain("No tasting experiences selected");
    }
}
