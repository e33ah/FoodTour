namespace FoodTour.Test.Controllers;

public class HomeControllerBookingConfirmationTests : HomeControllerTestBase {
    [Fact]
    public async Task BookingConfirmation_RedirectsToError_WhenNoTempData() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());

        var result = await sut.BookingConfirmation();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Error));
    }

    [Fact]
    public async Task BookingConfirmation_ReturnsView_WhenTempDataPresent() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            SelectedSupplierIds = [1, 2]
        };
        context.Bookings.Add(booking);
        context.SaveChanges();
        var sut = CreateConfiguredController(context);
        sut.TempData["BookingId"] = booking.Id;

        var result = await sut.BookingConfirmation();

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<BookingConfirmationVM>();
        model.CustomerEmail.ShouldBe(booking.CustomerEmail);
        model.PurchaseCode.ShouldBe(booking.PurchaseCode);
        model.TourDate.ShouldBe(booking.TourDate);
    }
}
