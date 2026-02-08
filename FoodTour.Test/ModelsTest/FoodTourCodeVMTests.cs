namespace FoodTour.Test.Models;

public class FoodTourCodeVMTests {
    [Fact]
    public void FoodTourCodeVM_InitialisesWithDefaultValues() {
        var sut = new FoodTourCodeVM();

        sut.TourDate.ShouldBe(default);
        sut.TourSuppliers.ShouldBeEmpty();
        sut.QrToken.ShouldBe(Guid.Empty);
    }
}
