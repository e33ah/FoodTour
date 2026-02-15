namespace FoodTour.Test.Controllers;

public class HomeControllerBookingSummaryTests : HomeControllerTestBase {
    [Fact]
    public async Task BookingSummary_PopulatesSelectedSuppliers_WhenIdsProvided() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var sut = CreateConfiguredController(context);

        var bookingSummary = new BookingSummaryVM { SelectedSupplierIds = [1, 3] };

        var result = await sut.BookingSummary(bookingSummary);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<BookingSummaryVM>();
        model.SelectedSuppliers.Count.ShouldBe(2);
        model.SelectedSuppliers.Select(s => s.Id).ShouldBe([1, 3]);
    }

    [Fact]
    public void ConfirmBookingSummary_RedirectsToIndex_WhenModelNull() {
        var sut = CreateController();

        var result = sut.ConfirmBookingSummary(null!);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public void ConfirmBookingSummary_ReturnsView_WhenModelStateInvalid() {
        var sut = CreateController();
        sut.ModelState.AddModelError("Test", "Invalid");

        var bookingSummary = TestHelper.CreateValidBookingSummaryVM([1, 2]);

        var result = sut.ConfirmBookingSummary(bookingSummary);

        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("BookingSummary");
    }

    [Fact]
    public void ConfirmBookingSummary_SetsTempDataAndRedirects_WhenModelValid() {
        var sut = CreateController();

        var bookingSummary = TestHelper.CreateValidBookingSummaryVM([1]);

        var result = sut.ConfirmBookingSummary(bookingSummary);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.BookingConfirmation));

        sut.TempData.ContainsKey("Confirmation").ShouldBeTrue();
        string? json = sut.TempData["Confirmation"] as string;
        json.ShouldNotBeNull();

        var confirmation = JsonSerializer.Deserialize<BookingConfirmationVM>(json!);
        confirmation!.CustomerEmail.ShouldBe(bookingSummary.CustomerEmail);
        confirmation.TourDate.ShouldBe(bookingSummary.TourDate);
    }
}