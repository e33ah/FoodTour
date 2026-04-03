namespace FoodTour.Test.Models;

public class TourCancelledVMTests {
    [Fact]
    public void TourCancelledVM_Initialises_WithDefaultValues() {
        var sut = new TourCancelledVM();

        sut.CustomerEmail.ShouldBe(string.Empty);
        sut.TourDate.ShouldBe(default(DateTime));
        sut.PurchaseCode.ShouldBe(string.Empty);
    }

    [Fact]
    public void TourCancelledVM_SetsProperties_Correctly() {
        string expectedEmail = "customer@example.com";
        DateTime expectedDate = new(2025, 7, 20);
        string expectedCode = "TOUR123";

        var sut = new TourCancelledVM {
            CustomerEmail = expectedEmail,
            TourDate = expectedDate,
            PurchaseCode = expectedCode
        };

        sut.CustomerEmail.ShouldBe(expectedEmail);
        sut.TourDate.ShouldBe(expectedDate);
        sut.PurchaseCode.ShouldBe(expectedCode);
    }

    [Theory]
    [InlineData("test@example.com", "2025-06-15", "ABC123")]
    [InlineData("", "2025-01-01", "")]
    [InlineData("another@test.com", "2025-12-31", "XYZ789")]
    public void TourCancelledVM_HandlesVariousValues_Correctly(string email, string dateString, string code) {
        DateTime date = DateTime.Parse(dateString);

        var sut = new TourCancelledVM {
            CustomerEmail = email,
            TourDate = date,
            PurchaseCode = code
        };

        sut.CustomerEmail.ShouldBe(email);
        sut.TourDate.ShouldBe(date);
        sut.PurchaseCode.ShouldBe(code);
    }
}
