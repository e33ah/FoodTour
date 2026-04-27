namespace FoodTour.Test.Controllers;

public class HomeControllerFoodTourCodeTests : HomeControllerTestBase {
    [Fact]
    public async Task FoodTourCode_ReturnsViewResult_WhenBookingExists() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.FoodTourCode(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourCodeVM>();
        model.TourDate.ShouldBe(TestHelper.validTourDate);
        model.TourSuppliers.Count.ShouldBe(1);
        var tourSupplier = model.TourSuppliers.Single();
        tourSupplier.Name.ShouldBe(supplier.Name);
        model.QrToken.ShouldBe(booking.QrToken);
        model.AllVisitsClaimed.ShouldBeFalse();
    }

    [Fact]
    public async Task FoodTourCode_RedirectsToAction_WhenBookingNotFound() {
        var sut = CreateController();

        var result = await sut.FoodTourCode(Guid.NewGuid());

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("Error");
    }

    [Fact]
    public async Task FoodTourCode_RedirectsToError_WhenQrTokenIsEmpty() {
        var sut = CreateController();

        var result = await sut.FoodTourCode(Guid.Empty);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("Error");
    }

    [Fact]
    public async Task FoodTourCode_RedirectsToTourCancelled_WhenBookingIsCancelled() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.IsCancelled = true;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);

        var sut = CreateConfiguredController(context);

        var result = await sut.FoodTourCode(booking.QrToken);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("TourCancelled");
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues["customerEmail"].ShouldBe(booking.CustomerEmail);
        redirectResult.RouteValues["tourDate"].ShouldBe(booking.TourDate);
        redirectResult.RouteValues["purchaseCode"].ShouldBe(booking.PurchaseCode);
    }

    [Fact]
    public async Task FoodTourCode_ReturnsCompletedState_WhenTourDateIsYesterday() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today.AddDays(-1);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.FoodTourCode(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourCodeVM>();
        model.QrToken.ShouldBe(booking.QrToken);
        model.AllVisitsClaimed.ShouldBeTrue();
    }

    [Fact]
    public async Task FoodTourCode_ReturnsViewResult_WhenTourDateIsToday() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today.AddHours(11);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.FoodTourCode(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourCodeVM>();
        model.QrToken.ShouldBe(booking.QrToken);
    }

    [Fact]
    public async Task FoodTourCode_SetsAllVisitsClaimedFalse_WhenNoVisitsExist() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.FoodTourCode(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourCodeVM>();
        model.AllVisitsClaimed.ShouldBeFalse();
    }

    [Fact]
    public async Task FoodTourCode_SetsAllVisitsClaimedFalse_WhenSomeVisitsNotClaimed() {
        var supplier1 = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var supplier2 = TestHelper.CreateSupplier(2, "Lovely Bites", true);
        var booking = TestHelper.CreateValidBooking([1, 2]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier1, supplier2], [booking]);

        var visit1 = new Visit { BookingId = booking.Id, SupplierId = 1, IsClaimed = true, ClaimedAt = DateTime.UtcNow, TastingExperiencesClaimed = 2 };
        var visit2 = new Visit { BookingId = booking.Id, SupplierId = 2, IsClaimed = false };
        await context.Visits.AddRangeAsync([visit1, visit2]);
        await context.SaveChangesAsync(CancellationToken);

        var sut = CreateConfiguredController(context);

        var result = await sut.FoodTourCode(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourCodeVM>();
        model.AllVisitsClaimed.ShouldBeFalse();
    }

    [Fact]
    public async Task FoodTourCode_SetsAllVisitsClaimedTrue_WhenAllVisitsClaimed() {
        var supplier1 = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var supplier2 = TestHelper.CreateSupplier(2, "Lovely Bites", true);
        var booking = TestHelper.CreateValidBooking([1, 2]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier1, supplier2], [booking]);

        var visit1 = new Visit { BookingId = booking.Id, SupplierId = 1, IsClaimed = true, ClaimedAt = DateTime.UtcNow, TastingExperiencesClaimed = 2 };
        var visit2 = new Visit { BookingId = booking.Id, SupplierId = 2, IsClaimed = true, ClaimedAt = DateTime.UtcNow, TastingExperiencesClaimed = 2 };
        await context.Visits.AddRangeAsync([visit1, visit2]);
        await context.SaveChangesAsync(CancellationToken);

        var sut = CreateConfiguredController(context);

        var result = await sut.FoodTourCode(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourCodeVM>();
        model.AllVisitsClaimed.ShouldBeTrue();
    }
}
