namespace FoodTour.Test.ServicesTest;

public class BookingEmailServiceTests {
    [Fact]
    public void GenerateBookingConfirmationEmail_IncludesFoodTourCodeLinkAndSupplierDetails_WhenUrlProvided() {
        var sut = new BookingEmailService();

        var booking = TestHelper.CreateValidBooking([1, 2]);

        var suppliers = new List<Supplier> {
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2)
        };

        const string FOOD_TOUR_CODE_URL = "https://example.test/Home/FoodTourCode?qrToken=abc";

        var (subject, body) = sut.GenerateBookingConfirmationEmail(booking, suppliers, FOOD_TOUR_CODE_URL);

        subject.ShouldContain("Weymouth Food Tour Booking Confirmation");
        body.ShouldContain("font-family: Arial");
        body.ShouldContain(FOOD_TOUR_CODE_URL);
        body.ShouldContain(TestHelper.SUPPLIER_NAME_1);
        body.ShouldContain(TestHelper.SUPPLIER_ADDRESS_1);
        body.ShouldContain("present your QR code");
    }

    [Fact]
    public void GenerateBookingConfirmationEmail_IncludesNoSelectionsMessage_WhenNoSuppliersProvided() {
        const string FOOD_TOUR_CODE_URL = "https://example.test/Home/FoodTourCode?qrToken=abc";
        var sut = new BookingEmailService();
        var booking = TestHelper.CreateValidBooking([]);
        var suppliers = Array.Empty<Supplier>();

        var (_, body) = sut.GenerateBookingConfirmationEmail(booking, suppliers, FOOD_TOUR_CODE_URL);

        body.ShouldContain("No tasting experiences selected");
    }
}
