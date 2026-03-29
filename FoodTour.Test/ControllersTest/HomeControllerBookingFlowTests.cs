namespace FoodTour.Test.Controllers;

public class HomeControllerBookingFlowTests : HomeControllerTestBase {

    [Fact]
    public async Task Index_SavesBookingImmediately_WhenValidInput() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.BookingSummary));

        // Verify booking was saved
        var bookings = await context.Bookings.ToListAsync();
        bookings.ShouldHaveSingleItem();
        var booking = bookings[0];
        booking.CustomerEmail.ShouldBe(TestHelper.VALID_EMAIL);
        booking.PurchaseCode.ShouldBe(TestHelper.VALID_CODE);
        booking.TourDate.ShouldBe(TestHelper.validTourDate);
        booking.SelectedSupplierIds.ShouldBe(selectedSuppliers);
        booking.IsConfirmed.ShouldBeFalse();
    }

    [Fact]
    public async Task Index_CreatesVisitRecords_WhenBookingSaved() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        var visits = await context.Visits.ToListAsync();
        visits.Count.ShouldBe(5);

        var booking = await context.Bookings.FirstOrDefaultAsync();
        booking.ShouldNotBeNull();

        foreach (int supplierId in selectedSuppliers) {
            var visit = visits.FirstOrDefault(v => v.SupplierId == supplierId);
            visit.ShouldNotBeNull();
            visit.BookingId.ShouldBe(booking.Id);
            visit.IsClaimed.ShouldBeFalse();
            visit.ClaimedAt.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Index_ReturnsBadRequest_WhenInvalidSupplierIds() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(2); // Only 2 suppliers exist
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = new List<int> { 1, 2, 99, 100, 101 }; // 99, 100, 101 don't exist

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<FoodTourVM>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldBe("One or more selected suppliers are invalid. Please refresh and try again.");
    }

    [Fact]
    public async Task BookingSummary_ReturnsView_WhenValidQrToken() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var booking = new Booking {
            QrToken = Guid.NewGuid(),
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3],
            IsConfirmed = false
        };
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);

        var result = await sut.BookingSummary(booking.QrToken);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<BookingSummaryVM>();
        model.QrToken.ShouldBe(booking.QrToken);
        model.CustomerEmail.ShouldBe(booking.CustomerEmail);
        model.SelectedSuppliers.Count.ShouldBe(3);
    }

    [Fact]
    public async Task BookingSummary_RedirectsToIndex_WhenEmptyQrToken() {
        var sut = CreateController();

        var result = await sut.BookingSummary(Guid.Empty);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public async Task BookingSummary_RedirectsToIndex_WhenBookingNotFound() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());

        var result = await sut.BookingSummary(Guid.NewGuid());

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
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();
        var sut = CreateConfiguredControllerWithUrl(context, "BookingConfirmation");

        var result = await sut.ConfirmBookingSummary(booking.QrToken);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.BookingConfirmation));

        // Verify booking was updated
        var updatedBooking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
        updatedBooking.ShouldNotBeNull();
        updatedBooking.IsConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmBookingSummary_RedirectsToIndex_WhenEmptyQrToken() {
        var sut = CreateController();

        var result = await sut.ConfirmBookingSummary(Guid.Empty);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public async Task ConfirmBookingSummary_RedirectsToConfirmation_WhenAlreadyConfirmed() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var booking = new Booking {
            QrToken = Guid.NewGuid(),
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3],
            IsConfirmed = true // Already confirmed
        };
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();
        var sut = CreateConfiguredControllerWithUrl(context, "BookingConfirmation");

        var result = await sut.ConfirmBookingSummary(booking.QrToken);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.BookingConfirmation));
        redirect.RouteValues!["qrToken"].ShouldBe(booking.QrToken);
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
        model.EmailWarning.ShouldContain("unable to send a confirmation email");
        model.EmailWarning.ShouldContain("http://localhost/Home/UserSupport");
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

    [Fact]
    public async Task BookingConfirmation_RedirectsToError_WhenEmptyToken() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());

        var result = await sut.BookingConfirmation(Guid.Empty);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Error));
    }

    [Fact]
    public async Task BookingConfirmation_RedirectsToError_WhenBookingNotFound() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());

        var result = await sut.BookingConfirmation(Guid.NewGuid());

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Error));
    }
}