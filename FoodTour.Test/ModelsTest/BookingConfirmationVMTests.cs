namespace FoodTour.Test.Models;

public class BookingConfirmationVMTests {

    [Fact]
    public void BookingConfirmationVM_InitialisesWithEmptyStrings() {
        var sut = new BookingConfirmationVM();

        sut.CustomerEmail.ShouldBe(string.Empty);
        sut.PurchaseCode.ShouldBe(string.Empty);
    }

    [Fact]
    public void BookingConfirmationVM_InitialisesWithDefaultValues() {
        var sut = new BookingConfirmationVM();

        sut.TourDate.ShouldBe(default);
        sut.SelectedSupplierIds.ShouldNotBeNull();
        sut.SelectedSupplierIds.ShouldBeEmpty();
        sut.BookingId.ShouldBe(0);
    }
}
