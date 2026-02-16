namespace FoodTour.Test.Models;

public class SupplierVisitVMTests {
    [Fact]
    public void Ctor_InitializesDefaults_WhenInstantiated() {
        var sut = new SupplierVisitVM();

        sut.Suppliers.ShouldNotBeNull();
        sut.Suppliers.Count.ShouldBe(0);
        sut.TourDate.ShouldBe(default);
        sut.CustomerEmail.ShouldBe(string.Empty);
        sut.SelectedSupplierId.ShouldBe(0);
        sut.SupplierCode.ShouldBe(string.Empty);
    }

    [Fact]
    public void Suppliers_SetAndRetrieved_WhenAssigned() {
        string supplier1Name = "ChipMe";
        string supplier2Name = "TasteBuds";
        var supplier1 = TestHelper.CreateSupplier(1, supplier1Name, true);
        var supplier2 = TestHelper.CreateSupplier(2, supplier2Name, true);
        var sut = new SupplierVisitVM {
            Suppliers = [supplier1, supplier2]
        };

        sut.Suppliers.Count.ShouldBe(2);
        sut.Suppliers.Select(s => s.Name).ShouldBe([supplier1Name, supplier2Name]);
    }
}
