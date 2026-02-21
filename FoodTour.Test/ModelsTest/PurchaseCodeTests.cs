namespace FoodTour.Test.Models;

public class PurchaseCodeTests {
    [Fact]
    public void PurchaseCode_InitialisesWithExpectedDefaults() {
        var before = DateTime.UtcNow;

        var sut = new PurchaseCode();

        var after = DateTime.UtcNow;

        sut.Code.ShouldBe(string.Empty);
        sut.IsActive.ShouldBeTrue();
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }
}
