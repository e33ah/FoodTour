namespace FoodTour.Test.Models;

public class FoodTourVMTests {
    [Fact]
    public void FoodTourVM_InitialisesWithEmptySuppliersList() {
        var sut = new FoodTourVM();

        sut.Suppliers.ShouldNotBeNull();
        sut.Suppliers.ShouldBeEmpty();
    }

    [Fact]
    public void FoodTourVM_InitialisesWithEmptyStrings() {
        var sut = new FoodTourVM();

        sut.SupplierName.ShouldBe(string.Empty);
        sut.SupplierDescription.ShouldBe(string.Empty);
        sut.SupplierAddress.ShouldBe(string.Empty);
    }

    [Fact]
    public void FoodTourVM_MaxSelections_CanBeSet() {
        var sut = new FoodTourVM { MaxSelections = 5 };

        sut.MaxSelections.ShouldBe(5);
    }

    [Fact]
    public void FoodTourVM_MaxSelections_DefaultsToZero() {
        var sut = new FoodTourVM();

        sut.MaxSelections.ShouldBe(0);
    }

    [Fact]
    public void FoodTourVM_CanSetSuppliersList() {
        string supplierOneName = "The Jolly Rodger";
        string supplierTwoName = "Pasta La Vista";
        var suppliers = new List<Supplier> {
            TestHelper.CreateSupplier(1, supplierOneName),
            TestHelper.CreateSupplier(2, supplierTwoName)
        };

        var sut = new FoodTourVM { Suppliers = suppliers };

        sut.Suppliers.Count.ShouldBe(2);
        sut.Suppliers[0].Name.ShouldBe(supplierOneName);
        sut.Suppliers[1].Name.ShouldBe(supplierTwoName);
    }

    [Fact]
    public void FoodTourVM_CanSetStringProperties() {
        var sut = new FoodTourVM {
            SupplierName = "Sea Snacks",
            SupplierDescription = "A great tasting experience",
            SupplierAddress = "123 Main Street"
        };

        sut.SupplierName.ShouldBe("Sea Snacks");
        sut.SupplierDescription.ShouldBe("A great tasting experience");
        sut.SupplierAddress.ShouldBe("123 Main Street");
    }
}
