namespace FoodTour.Test.Controllers;

public class HomeControllerBookingConfirmationTests : HomeControllerTestBase {
    [Fact]
    public async Task BookingConfirmation_RedirectsToError_WhenEmptyToken() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());

        var result = await sut.BookingConfirmation(Guid.Empty);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Error));
    }

    [Fact]
    public async Task BookingConfirmation_ReturnsView_WhenValidBookingExists() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerName = TestHelper.DEFAULT_CUSTOMER_NAME, 
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            SelectedSupplierIds = [1, 2],
            QrToken = Guid.NewGuid()
        };
        context.Bookings.Add(booking);
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.BookingConfirmation(booking.QrToken);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<BookingConfirmationVM>();
        model.CustomerEmail.ShouldBe(booking.CustomerEmail);
        model.CustomerName.ShouldBe(booking.CustomerName);
        model.QrToken.ShouldBe(booking.QrToken);
    }

    [Fact]
    public async Task BookingConfirmation_SetsEmailWarningProperty_WhenEmailWarningIsTrue() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            SelectedSupplierIds = [1, 2],
            QrToken = Guid.NewGuid()
        };
        context.Bookings.Add(booking);
        context.SaveChanges();
        var sut = CreateConfiguredControllerWithUrl(context, "UserSupport");

        var result = await sut.BookingConfirmation(booking.QrToken, emailWarning: true);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<BookingConfirmationVM>();
        model.EmailWarning.ShouldNotBeNull();
        model.EmailWarning.ShouldBe("Your booking was saved, but we were unable to send a confirmation email. Please save the link above with your tour details and <a href='http://localhost/Home/UserSupport'>contact support</a> for assistance.");
    }

    [Fact]
    public async Task BookingConfirmation_DoesNotSetEmailWarningProperty_WhenEmailWarningIsFalse() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            SelectedSupplierIds = [1, 2],
            QrToken = Guid.NewGuid()
        };
        context.Bookings.Add(booking);
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.BookingConfirmation(booking.QrToken, emailWarning: false);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<BookingConfirmationVM>();
        model.EmailWarning.ShouldBeNull();
    }
}
