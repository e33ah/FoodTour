namespace FoodTour.Test.Controllers;

public class HomeControllerIndexTests : HomeControllerTestBase {
    [Fact]
    public async Task Index_ReturnsViewResult_WithActiveSuppliers() {
        string name1 = "The Enchanted Spoon";
        string name2 = "Whisker's Tea Rooms";
        var suppliers = new[] {
            TestHelper.CreateSupplier(1, name1, address: "123 Test St"),
            TestHelper.CreateSupplier(2, name2, address: "456 Test Ave")
        };

        var (_, model) = await GetIndexViewModel(suppliers);

        model.Suppliers.Count.ShouldBe(2);
        model.Suppliers[0].Name.ShouldBe(name1);
        model.Suppliers[1].Name.ShouldBe(name2);
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithOnlyActiveSuppliers() {
        var suppliers = new[] {
            TestHelper.CreateSupplier(1, "Active Supplier", isActive: true),
            TestHelper.CreateSupplier(2, "Inactive Supplier", isActive: false)
        };

        var (_, model) = await GetIndexViewModel(suppliers);

        model.Suppliers.Count.ShouldBe(1);
        model.Suppliers[0].Name.ShouldBe("Active Supplier");
        model.Suppliers[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Index_SetsMaxSelections_InViewModel() {
        var (_, model) = await GetIndexViewModel();

        model.MaxSelections.ShouldBe(TestHelper.EXPECTED_MAX_SELECTIONS);
    }

    [Fact]
    public async Task Index_SetsNoSuppliersMessage_WhenNoActiveSuppliers() {
        var (viewResult, model) = await GetIndexViewModel();

        model.Suppliers.Count.ShouldBe(0);
        viewResult.ViewData[TestHelper.NO_SUPPLIERS_MESSAGE_KEY].ShouldBe(TestHelper.EXPECTED_NO_SUPPLIERS_MESSAGE);
    }

    [Fact]
    public async Task Index_DoesNotSetMessages_WhenAtLeastMaxActiveSuppliersExist() {
        var (viewResult, _) = await GetIndexViewModel(supplierCount: TestHelper.EXPECTED_MAX_SELECTIONS);

        viewResult.ViewData.ContainsKey(TestHelper.NO_SUPPLIERS_MESSAGE_KEY).ShouldBeFalse();
        viewResult.ViewData.ContainsKey(TestHelper.INSUFFICIENT_SUPPLIERS_WARNING_KEY).ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Index_SetsInsufficientSuppliersWarning_WhenFewerThanMaxActiveSuppliers(int supplierCount) {
        var (viewResult, model) = await GetIndexViewModel(supplierCount: supplierCount);

        model.Suppliers.Count.ShouldBe(supplierCount);

        string experienceText = supplierCount == 1 ? "tasting experience is" : "tasting experiences are";
        string expectedMessage = $"Only {supplierCount} {experienceText} currently available. You need at least {TestHelper.EXPECTED_MAX_SELECTIONS} to create a tour. Please check back later.";

        viewResult.ViewData[TestHelper.INSUFFICIENT_SUPPLIERS_WARNING_KEY].ShouldBe(expectedMessage);
        viewResult.ViewData.ContainsKey(TestHelper.NO_SUPPLIERS_MESSAGE_KEY).ShouldBeFalse();
    }

    [Fact]
    public async Task Index_DoesNotSetInsufficientSuppliersWarning_WhenExactlyMaxActiveSuppliers() {
        var (viewResult, _) = await GetIndexViewModel(supplierCount: TestHelper.EXPECTED_MAX_SELECTIONS);

        viewResult.ViewData.ContainsKey(TestHelper.INSUFFICIENT_SUPPLIERS_WARNING_KEY).ShouldBeFalse();
    }

    private static async Task<(ViewResult viewResult, FoodTourVM model)> GetIndexViewModel(params Supplier[] suppliers) {
        var context = suppliers.Length > 0
            ? TestHelper.CreateContextWithSuppliers(suppliers)
            : TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateHomeController(context);

        var result = await sut.Index(null, null, null, null);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();
        return (viewResult, model);
    }

    private static async Task<(ViewResult viewResult, FoodTourVM model)> GetIndexViewModel(int supplierCount) {
        var suppliers = Enumerable.Range(1, supplierCount)
            .Select(i => TestHelper.CreateSupplier(i, $"Test Supplier {i}"))
            .ToArray();
        return await GetIndexViewModel(suppliers);
    }

    [Fact]
    public async Task Index_PopulatesForm_WhenParametersProvided() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateHomeController(context);
        var tourDate = new DateTime(2024, 6, 15);
        string customerEmail = "test@example.com";
        string purchaseCode = "TEST123";
        string selectedSuppliers = "1,2,3";

        var result = await sut.Index(tourDate, customerEmail, purchaseCode, selectedSuppliers);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();

        model.TourDate.ShouldBe(tourDate);
        model.CustomerEmail.ShouldBe(customerEmail);
        model.CustomerEmailConfirmation.ShouldBe(customerEmail);
        model.PurchaseCode.ShouldBe(purchaseCode);

        var selectedSuppliersList = (List<int>)viewResult.ViewData["SelectedSuppliers"]!;
        selectedSuppliersList.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Index_HandlesEmptyParameters_WithoutErrors() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateHomeController(context);

        var result = await sut.Index(
            tourDate: (DateTime?)null,
            customerEmail: (string?)null,
            purchaseCode: (string?)null,
            selectedSuppliers: (string?)null);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();

        model.TourDate.ShouldBeNull();
        model.CustomerEmail.ShouldBe(string.Empty);
        model.CustomerEmailConfirmation.ShouldBe(string.Empty);
        model.PurchaseCode.ShouldBe(string.Empty);
    }
}