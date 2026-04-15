namespace FoodTour.Test.Controllers;

public class HomeControllerBookingSummaryTests : HomeControllerTestBase {
    [Fact]
    public async Task BookingSummary_ReturnsView_WhenValidBookingExists() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var booking = new Booking {
            QrToken = Guid.NewGuid(),
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 3],
            IsConfirmed = false
        };
        await context.Bookings.AddAsync(booking, CancellationToken);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateConfiguredController(context);

        var result = await sut.BookingSummary(booking.QrToken);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<BookingSummaryVM>();
        model.SelectedSuppliers.Count.ShouldBe(2);
        model.SelectedSuppliers.Select(s => s.Id).ShouldBe([1, 3]);
        model.QrToken.ShouldBe(booking.QrToken);
    }

    [Fact]
    public async Task BookingSummary_RedirectsToIndex_WhenEmptyToken() {
        var sut = CreateController();

        var result = await sut.BookingSummary(Guid.Empty);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public async Task ConfirmBookingSummary_RedirectsToIndex_WhenEmptyToken() {
        var sut = CreateController();

        var result = await sut.ConfirmBookingSummary(Guid.Empty);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public async Task ConfirmBookingSummary_UpdatesIsConfirmedAndRedirects_WhenValidBooking() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var booking = new Booking {
            QrToken = Guid.NewGuid(),
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3],
            IsConfirmed = false
        };
        await context.Bookings.AddAsync(booking, CancellationToken);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateConfiguredControllerWithUrl(context, "BookingConfirmation");

        var result = await sut.ConfirmBookingSummary(booking.QrToken);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.BookingConfirmation));

        // Verify booking was updated
        var updatedBooking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id, CancellationToken);
        updatedBooking.ShouldNotBeNull();
        updatedBooking.IsConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateBookingSummaryErrorViewAsync_AddsErrorToModelState_WhenCalled() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var booking = new Booking {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2],
            QrToken = Guid.NewGuid()
        };
        await context.Bookings.AddAsync(booking, CancellationToken);
        await context.SaveChangesAsync(CancellationToken);
        var controller = CreateConfiguredController(context);

        // Use reflection to call the private method for testing
        var method = typeof(HomeController).GetMethod("CreateBookingSummaryErrorViewAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull("Method should exist");
        object? invokeResult = method.Invoke(controller, [booking, "Test error message"]);
        invokeResult.ShouldNotBeNull("Method should return a result");
        var result = await (Task<IActionResult>)invokeResult;

        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("BookingSummary");
        var model = view.Model.ShouldBeOfType<BookingSummaryVM>();
        model.QrToken.ShouldBe(booking.QrToken);

        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        controller.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldBe("Test error message");
    }
}
