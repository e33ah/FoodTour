namespace FoodTour.Test.Controllers;

public class HomeControllerTourCancelledTests : HomeControllerTestBase {
    [Fact]
    public void TourCancelled_ReturnsViewResult_WithEmptyModel_WhenNoParametersProvided() {
        var sut = CreateController();

        var result = sut.TourCancelled();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TourCancelledVM>();
        model.CustomerEmail.ShouldBe(string.Empty);
        model.TourDate.ShouldBe(DateTime.MinValue);
        model.PurchaseCode.ShouldBe(string.Empty);
    }

    [Fact]
    public void TourCancelled_ReturnsViewResult_WithPopulatedModel_WhenParametersProvided() {
        var sut = CreateController();
        string customerEmail = "test@example.com";
        DateTime tourDate = new(2025, 6, 15);
        string purchaseCode = "ABC123";

        var result = sut.TourCancelled(customerEmail, tourDate, purchaseCode);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TourCancelledVM>();
        model.CustomerEmail.ShouldBe(customerEmail);
        model.TourDate.ShouldBe(tourDate);
        model.PurchaseCode.ShouldBe(purchaseCode);
    }

    [Fact]
    public void TourCancelled_ReturnsViewResult_WithPartialModel_WhenSomeParametersNull() {
        var sut = CreateController();
        string customerEmail = "test@example.com";

        var result = sut.TourCancelled(customerEmail, null, null);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TourCancelledVM>();
        model.CustomerEmail.ShouldBe(customerEmail);
        model.TourDate.ShouldBe(DateTime.MinValue);
        model.PurchaseCode.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData(null, "2025-06-15", "ABC123")]
    [InlineData("test@example.com", null, "ABC123")]
    [InlineData("test@example.com", "2025-06-15", null)]
    public void TourCancelled_HandlesNullParameters_Gracefully(string? customerEmail, string? tourDateString, string? purchaseCode) {
        var sut = CreateController();
        DateTime? tourDate = string.IsNullOrEmpty(tourDateString) ? null : DateTime.Parse(tourDateString);

        var result = sut.TourCancelled(customerEmail, tourDate, purchaseCode);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TourCancelledVM>();
        model.ShouldNotBeNull();
    }
}
