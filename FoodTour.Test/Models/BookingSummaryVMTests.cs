namespace FoodTour.Test.Models;

public class BookingSummaryVMTests {
    [Fact]
    public void BookingSummaryVM_InitialisesWithEmptySuppliersList() {
        var sut = new BookingSummaryVM();

        sut.SelectedSupplierIds.ShouldNotBeNull();
        sut.SelectedSupplierIds.ShouldBeEmpty();
        sut.SelectedSuppliers.ShouldNotBeNull();
        sut.SelectedSuppliers.ShouldBeEmpty();
    }

    [Fact]
    public void BookingSummaryVM_InitialisesWithEmptyStrings() {
        var sut = new BookingSummaryVM();

        sut.CustomerEmail.ShouldBe(string.Empty);
        sut.PurchaseCode.ShouldBe(string.Empty);
    }
}
