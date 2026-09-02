namespace FoodTour.Test.Models;

public class BookingCheckRequestTests {
    [Fact]
    public void BookingCheckRequest_ShouldInitializeWithDefaultValues() {
        var bookingCheckRequest = new BookingCheckRequest();
        bookingCheckRequest.CustomerEmail.ShouldBeEmpty();
        bookingCheckRequest.PurchaseCode.ShouldBeEmpty();
    }
}
