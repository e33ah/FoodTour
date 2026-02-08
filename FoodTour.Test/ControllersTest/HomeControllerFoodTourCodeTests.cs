namespace FoodTour.Test.Controllers;

public class HomeControllerFoodTourCodeTests : HomeControllerTestBase {
    [Fact]
    public async Task FoodTourCode_ReturnsViewResult_WhenBookingExists() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateControllerWithTempData(context);

        var result = await sut.FoodTourCode(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourCodeVM>();
        model.TourDate.ShouldBe(TestHelper.validTourDate);
        model.TourSuppliers.Count.ShouldBe(1);
        var tourSupplier = model.TourSuppliers.Single();
        tourSupplier.Name.ShouldBe(supplier.Name);
        model.QrToken.ShouldBe(booking.QrToken);
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
    public async Task FoodTourCode_RedirectsToError_WhenBookingIsCancelled() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.IsCancelled = true;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);

        var sut = CreateControllerWithTempData(context);

        var result = await sut.FoodTourCode(booking.QrToken);
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("Error");
    }
}
