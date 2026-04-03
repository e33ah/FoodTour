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
        sut.SupplierOpeningTimes.ShouldBe(string.Empty);
        sut.CustomerEmail.ShouldBe(string.Empty);
        sut.PurchaseCode.ShouldBe(string.Empty);
    }

    [Fact]
    public void FoodTourVM_InitialisesWithEmptySelectedSuppliersList() {
        var sut = new FoodTourVM();

        sut.SelectedSuppliers.ShouldNotBeNull();
        sut.SelectedSuppliers.ShouldBeEmpty();
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
    public void FoodTourVM_CanSetSelectedSuppliersList() {
        var selectedSuppliers = new List<int> { 1, 2, 3, 4, 5 };

        var sut = new FoodTourVM { SelectedSuppliers = selectedSuppliers };

        sut.SelectedSuppliers.Count.ShouldBe(5);
        sut.SelectedSuppliers.ShouldBe(selectedSuppliers);
    }

    [Fact]
    public void FoodTourVM_CanSetCustomerEmail() {
        string email = "test@example.com";

        var sut = new FoodTourVM { CustomerEmail = email };

        sut.CustomerEmail.ShouldBe(email);
    }

    [Fact]
    public void FoodTourVM_CanSetPurchaseCode() {
        string purchaseCode = "123123123";

        var sut = new FoodTourVM { PurchaseCode = purchaseCode };

        sut.PurchaseCode.ShouldBe(purchaseCode);
    }

    [Fact]
    public void FoodTourVM_NoSuppliersMessage_DefaultsToNull() {
        var sut = new FoodTourVM();

        sut.NoSuppliersMessage.ShouldBeNull();
    }

    [Fact]
    public void FoodTourVM_InsufficientSuppliersWarning_DefaultsToNull() {
        var sut = new FoodTourVM();

        sut.InsufficientSuppliersWarning.ShouldBeNull();
    }

    [Fact]
    public void FoodTourVM_CanSetNoSuppliersMessage() {
        string message = "No tasting experiences available.";

        var sut = new FoodTourVM { NoSuppliersMessage = message };

        sut.NoSuppliersMessage.ShouldBe(message);
    }

    [Fact]
    public void FoodTourVM_CanSetInsufficientSuppliersWarning() {
        string warning = "Only 3 suppliers available.";

        var sut = new FoodTourVM { InsufficientSuppliersWarning = warning };

        sut.InsufficientSuppliersWarning.ShouldBe(warning);
    }

    [Fact]
    public void FoodTourVM_CanSetStringProperties() {
        var sut = new FoodTourVM {
            SupplierName = "Sea Snacks",
            SupplierDescription = "A great tasting experience",
            SupplierAddress = "123 Main Street",
            SupplierOpeningTimes = "9am - 5pm"
        };

        sut.SupplierName.ShouldBe("Sea Snacks");
        sut.SupplierDescription.ShouldBe("A great tasting experience");
        sut.SupplierAddress.ShouldBe("123 Main Street");
        sut.SupplierOpeningTimes.ShouldBe("9am - 5pm");
    }
}
