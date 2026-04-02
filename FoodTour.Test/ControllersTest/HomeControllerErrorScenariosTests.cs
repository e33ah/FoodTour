namespace FoodTour.Test.Controllers;

public class HomeControllerErrorScenariosTests : HomeControllerTestBase {

    [Fact]
    public async Task Index_ReturnsErrorView_WhenDatabaseSaveThrows() {
        using var context = TestHelper.CreateContextFailingOnSave();
        // Add required suppliers and purchase codes to failing context before it starts failing
        var suppliers = Enumerable.Range(1, 5).Select(i => TestHelper.CreateSupplier(i)).ToArray();
        context.Suppliers.AddRange(suppliers);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        context.SaveChanges(); // This should work
        
        var sut = CreateConfiguredControllerWithUrl(context, "UserSupport");
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<FoodTourVM>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldContain("Unable to save your booking due to a system error");
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldContain("contact support");
    }

    [Fact]
    public async Task ConfirmBookingSummary_ReturnsErrorView_WhenDatabaseUpdateThrows() {
        using var failingContext = TestHelper.CreateContextFailingOnSave();
        
        // First, create booking in a normal context
        var normalContext = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var booking = new Booking {
            QrToken = Guid.NewGuid(),
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3],
            IsConfirmed = false
        };
        await normalContext.Bookings.AddAsync(booking);
        await normalContext.SaveChangesAsync();
        
        // Now set up failing context with the same data
        var suppliers = Enumerable.Range(1, 3).Select(i => TestHelper.CreateSupplier(i)).ToArray();
        failingContext.Suppliers.AddRange(suppliers);
        failingContext.Bookings.Add(new Booking {
            Id = booking.Id,
            QrToken = booking.QrToken,
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            PurchaseCode = booking.PurchaseCode,
            SelectedSupplierIds = booking.SelectedSupplierIds,
            IsConfirmed = false
        });
        failingContext.SaveChanges(); // This works
        
        var sut = CreateConfiguredControllerWithUrl(failingContext, "UserSupport");

        var result = await sut.ConfirmBookingSummary(booking.QrToken);

        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("BookingSummary");
        var model = view.Model.ShouldBeOfType<BookingSummaryVM>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldContain("Unable to confirm your booking due to a system error");
    }

    [Fact]
    public async Task ConfirmBookingSummary_RedirectsWithEmailWarning_WhenEmailSendingFails() {
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
        
        // Use controller with null URL to simulate email sending failure
        var sut = CreateConfiguredControllerWithUrl(context, null);

        var result = await sut.ConfirmBookingSummary(booking.QrToken);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.BookingConfirmation));
        redirect.RouteValues!["qrToken"].ShouldBe(booking.QrToken);
        redirect.RouteValues["emailWarning"].ShouldBe(true);

        // Verify booking was still marked as confirmed despite email failure
        var updatedBooking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
        updatedBooking.ShouldNotBeNull();
        updatedBooking.IsConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task BookingSummary_RedirectsToIndex_WhenBookingIsCancelled() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var booking = new Booking {
            QrToken = Guid.NewGuid(),
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3],
            IsConfirmed = false,
            IsCancelled = true
        };
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);

        var result = await sut.BookingSummary(booking.QrToken);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public async Task BookingConfirmation_RedirectsToError_WhenBookingIsCancelled() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            SelectedSupplierIds = [1, 2],
            QrToken = Guid.NewGuid(),
            IsCancelled = true
        };
        context.Bookings.Add(booking);
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.BookingConfirmation(booking.QrToken);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Error));
    }

    [Fact]
    public async Task Index_SetsFocusPurchaseCodeFlag_WhenPurchaseCodeIsInvalid() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, "INVALID_CODE");

        var view = result.ShouldBeOfType<ViewResult>();
        var viewBag = view.ViewData;
        viewBag["FocusPurchaseCode"].ShouldBe(true);
    }

    [Fact]
    public async Task Index_PreservesUserInputInModel_WhenValidationFails() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);
        string testEmail = "test@example.com";
        string testPurchaseCode = "TEST_CODE";

        var result = await sut.Index(null, selectedSuppliers, testEmail, testPurchaseCode);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<FoodTourVM>();
        model.CustomerEmail.ShouldBe(testEmail);
        model.PurchaseCode.ShouldBe(testPurchaseCode);
        view.ViewData["SelectedSuppliers"].ShouldBe(selectedSuppliers);
    }
}
