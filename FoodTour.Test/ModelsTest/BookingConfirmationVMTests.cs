namespace FoodTour.Test.Models;

public class BookingConfirmationVMTests {
    [Fact]
    public void BookingConfirmationVM_InitialisesWithDefaultValues() {
        var sut = new BookingConfirmationVM();

        sut.CustomerEmail.ShouldBe(string.Empty);
        sut.CustomerName.ShouldBe(string.Empty);
        sut.QrToken.ShouldBe(Guid.Empty);
        sut.EmailWarning.ShouldBeNull();
    }
}
