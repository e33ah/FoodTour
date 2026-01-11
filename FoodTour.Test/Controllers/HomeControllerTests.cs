namespace FoodTour.Test.Controllers;

public class HomeControllerTests {
    private const int EXPECTED_MAX_SELECTIONS = 5;
    private const string NO_SUPPLIERS_MESSAGE_KEY = "NoSuppliersMessage";
    private const string INSUFFICIENT_SUPPLIERS_WARNING_KEY = "InsufficientSuppliersWarning";
    private const string EXPECTED_NO_SUPPLIERS_MESSAGE = "No tasting experiences are currently available for the tour. Please check back later.";
    private static readonly int nextYear = DateTime.Today.AddYears(1).Year;
    private static readonly DateTime validTourDate = new(nextYear, 6, 16);

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

        model.MaxSelections.ShouldBe(EXPECTED_MAX_SELECTIONS);
    }

    [Fact]
    public async Task Index_SetsNoSuppliersMessage_WhenNoActiveSuppliers() {
        var (viewResult, model) = await GetIndexViewModel();

        model.Suppliers.Count.ShouldBe(0);
        viewResult.ViewData[NO_SUPPLIERS_MESSAGE_KEY].ShouldBe(EXPECTED_NO_SUPPLIERS_MESSAGE);
    }

    [Fact]
    public async Task Index_DoesNotSetMessages_WhenAtLeastMaxActiveSuppliersExist() {
        var (viewResult, _) = await GetIndexViewModel(supplierCount: EXPECTED_MAX_SELECTIONS);

        viewResult.ViewData.ContainsKey(NO_SUPPLIERS_MESSAGE_KEY).ShouldBeFalse();
        viewResult.ViewData.ContainsKey(INSUFFICIENT_SUPPLIERS_WARNING_KEY).ShouldBeFalse();
    }

    [Theory]
    [InlineData(1, "Only 1 tasting experience is currently available. You need at least 5 to create a tour. Please check back later.")]
    [InlineData(2, "Only 2 tasting experiences are currently available. You need at least 5 to create a tour. Please check back later.")]
    [InlineData(3, "Only 3 tasting experiences are currently available. You need at least 5 to create a tour. Please check back later.")]
    [InlineData(4, "Only 4 tasting experiences are currently available. You need at least 5 to create a tour. Please check back later.")]
    public async Task Index_SetsInsufficientSuppliersWarning_WhenFewerThanMaxActiveSuppliers(int supplierCount, string expectedMessage) {
        var (viewResult, model) = await GetIndexViewModel(supplierCount: supplierCount);

        model.Suppliers.Count.ShouldBe(supplierCount);
        viewResult.ViewData[INSUFFICIENT_SUPPLIERS_WARNING_KEY].ShouldBe(expectedMessage);
        viewResult.ViewData.ContainsKey(NO_SUPPLIERS_MESSAGE_KEY).ShouldBeFalse();
    }

    [Fact]
    public async Task Index_DoesNotSetInsufficientSuppliersWarning_WhenExactlyMaxActiveSuppliers() {
        var (viewResult, _) = await GetIndexViewModel(supplierCount: EXPECTED_MAX_SELECTIONS);

        viewResult.ViewData.ContainsKey(INSUFFICIENT_SUPPLIERS_WARNING_KEY).ShouldBeFalse();
    }

    [Fact]
    public async Task IndexPost_RedirectsToIndex_WhenSelectionsAreValid() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(EXPECTED_MAX_SELECTIONS);
        var validTourDate = HomeControllerTests.validTourDate;

        var result = await PostIndex(validTourDate, selectedSuppliers, EXPECTED_MAX_SELECTIONS);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public async Task IndexPost_ReturnsViewWithError_WhenTooManySelections() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(6);
        var validTourDate = HomeControllerTests.validTourDate;

        var (result, controller) = await PostIndexWithController(validTourDate, selectedSuppliers, EXPECTED_MAX_SELECTIONS + 1);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
    }

    [Fact]
    public async Task IndexPost_AcceptsExactlyFiveSelections() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(EXPECTED_MAX_SELECTIONS);
        var validTourDate = HomeControllerTests.validTourDate;

        var result = await PostIndex(validTourDate, selectedSuppliers, EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task IndexPost_RejectsInvalidSelectionCounts(int selectionCount) {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(selectionCount);
        int contextSupplierCount = Math.Max(selectionCount, EXPECTED_MAX_SELECTIONS);
        var validTourDate = HomeControllerTests.validTourDate;

        var (result, controller) = await PostIndexWithController(validTourDate, selectedSuppliers, contextSupplierCount);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task IndexPost_ReturnsViewWithError_WhenTourDateIsSunday() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(EXPECTED_MAX_SELECTIONS);
        var sundayDate = new DateTime(nextYear, 6, 1);
        if (sundayDate.DayOfWeek != DayOfWeek.Sunday) {
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)sundayDate.DayOfWeek + 7) % 7;
            sundayDate = sundayDate.AddDays(daysUntilSunday);
        }

        var (result, controller) = await PostIndexWithController(sundayDate, selectedSuppliers, EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
    }

    [Fact]
    public async Task IndexPost_ReturnsViewWithError_WhenTourDateIsNull() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(EXPECTED_MAX_SELECTIONS);

        var (result, controller) = await PostIndexWithController(null, selectedSuppliers, EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(11)]
    [InlineData(12)]
    public async Task IndexPost_ReturnsViewWithError_WhenTourDateIsOutsideSeason(int month) {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(EXPECTED_MAX_SELECTIONS);
        var outOfSeasonDate = new DateTime(nextYear, month, 15);

        var (result, controller) = await PostIndexWithController(outOfSeasonDate, selectedSuppliers, EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(3, 15)]
    [InlineData(6, 20)]
    [InlineData(10, 30)]
    public async Task IndexPost_AcceptsValidDatesWithinSeason(int month, int day) {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(EXPECTED_MAX_SELECTIONS);
        var validDate = new DateTime(nextYear, month, day);
        if (validDate.DayOfWeek == DayOfWeek.Sunday) { 
            validDate = validDate.AddDays(1);
        }

        var result = await PostIndex(validDate, selectedSuppliers, EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Fact]
    public void Privacy_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.Privacy();

        result.ShouldBeOfType<ViewResult>();
    }

    private static async Task<(ViewResult viewResult, FoodTourVM model)> GetIndexViewModel(params Supplier[] suppliers) {
        var context = suppliers.Length > 0 
            ? TestHelper.CreateContextWithSuppliers(suppliers) 
            : TestHelper.CreateInMemoryContext();
        var sut = new HomeController(context);

        var result = await sut.Index();

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

    private static async Task<IActionResult> PostIndex(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(contextSupplierCount);
        var sut = new HomeController(context);
        return await sut.Index(tourDate, selectedSuppliers);
    }

    private static async Task<(IActionResult result, HomeController controller)> PostIndexWithController(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(contextSupplierCount);
        var sut = new HomeController(context);
        var result = await sut.Index(tourDate, selectedSuppliers);
        return (result, sut);
    }

    private static HomeController CreateController() {
        return new HomeController(TestHelper.CreateInMemoryContext());
    }
}
