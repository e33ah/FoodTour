namespace FoodTour.Test.Controllers;

public class HomeControllerTests {
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

    [Fact]
    public async Task IndexPost_RedirectsToBookingSummary_WhenSelectionsAreValid() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);
        var validTourDate = TestHelper.validTourDate;

        var result = await PostIndex(validTourDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.BookingSummary));
    }

    [Fact]
    public async Task IndexPost_ReturnsViewWithError_WhenTooManySelections() {
        int overMaxCount = TestHelper.EXPECTED_MAX_SELECTIONS + 1;
        var selectedSuppliers = TestHelper.CreateSupplierIdList(overMaxCount);
        var validTourDate = TestHelper.validTourDate;

        var (result, controller) = await PostIndexWithController(validTourDate, selectedSuppliers, overMaxCount);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
    }

    [Fact]
    public async Task IndexPost_AcceptsExactlyFiveSelections() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);
        var validTourDate = TestHelper.validTourDate;

        var result = await PostIndex(validTourDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS);

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
        int contextSupplierCount = Math.Max(selectionCount, TestHelper.EXPECTED_MAX_SELECTIONS);

        var (result, controller) = await PostIndexWithController(TestHelper.validTourDate, selectedSuppliers, contextSupplierCount);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task IndexPost_ReturnsViewWithError_WhenTourDateIsSunday() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);
        var sundayDate = new DateTime(TestHelper.nextYear, 6, 1);
        if (sundayDate.DayOfWeek != DayOfWeek.Sunday) {
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)sundayDate.DayOfWeek + 7) % 7;
            sundayDate = sundayDate.AddDays(daysUntilSunday);
        }

        var (result, controller) = await PostIndexWithController(sundayDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
    }

    [Fact]
    public async Task IndexPost_ReturnsViewWithError_WhenTourDateIsInThePast() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);
        var pastDate = TestHelper.validTourDate.AddYears(-2);

        var (result, controller) = await PostIndexWithController(pastDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
    }

    [Fact]
    public async Task IndexPost_ReturnsViewWithError_WhenTourDateIsNull() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);

        var (result, controller) = await PostIndexWithController(null, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS);

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
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);
        var outOfSeasonDate = new DateTime(TestHelper.nextYear, month, 15);

        var (result, controller) = await PostIndexWithController(outOfSeasonDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(3, 15)]
    [InlineData(6, 20)]
    [InlineData(10, 30)]
    public async Task IndexPost_AcceptsValidDatesWithinSeason(int month, int day) {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);
        var validDate = new DateTime(TestHelper.nextYear, month, day);
        if (validDate.DayOfWeek == DayOfWeek.Sunday) {
            validDate = validDate.AddDays(1);
        }

        var result = await PostIndex(validDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IndexPost_ReturnsViewWithError_WhenEmailIsMissing(string customerEmail) {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);

        var (result, controller) = await PostIndexWithController(TestHelper.validTourDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS, customerEmail, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.CustomerEmail)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing_at_sign.com")]
    [InlineData("missing_domain@")]
    public async Task IndexPost_ReturnsViewWithError_WhenEmailIsInvalid(string email) {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);

        var (result, controller) = await PostIndexWithController(TestHelper.validTourDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS, email, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.CustomerEmail)).ShouldBeTrue();
    }

    [Fact]
    public async Task IndexPost_AcceptsValidEmail_WhenAllDataIsValid() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);
        var validTourDate = TestHelper.validTourDate;

        var result = await PostIndex(validTourDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS);

        result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task IndexPost_ReturnsViewWithError_WhenCodeMissing() {
        var selectedSuppliers = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS);

        var (result, controller) = await PostIndexWithController(TestHelper.validTourDate, selectedSuppliers, TestHelper.EXPECTED_MAX_SELECTIONS, TestHelper.VALID_EMAIL, "");

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.PurchaseCode)).ShouldBeTrue();
    }

    [Fact]
    public void Privacy_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.Privacy();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsViewResult_WithErrorViewModel() {
        var sut = CreateController();
        var httpContext = new DefaultHttpContext { TraceIdentifier = "TestTraceId" };
        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var result = sut.Error();
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ErrorViewModel>();
        model.RequestId.ShouldBe("TestTraceId");
    }

    [Fact]
    public async Task BookingSummary_RedirectsToIndex_WhenNullModel() {
        var sut = CreateControllerWithTempData(TestHelper.CreateInMemoryContext());

        var result = await sut.BookingSummary(null!);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public async Task BookingSummary_PopulatesSelectedSuppliers_WhenIdsProvided() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var sut = CreateControllerWithTempData(context);

        var bookingSummary = new BookingSummaryVM { SelectedSupplierIds = [1, 3] };

        var result = await sut.BookingSummary(bookingSummary);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<BookingSummaryVM>();
        model.SelectedSuppliers.Count.ShouldBe(2);
        model.SelectedSuppliers.Select(s => s.Id).ShouldBe([1, 3]);
    }

    [Fact]
    public void ConfirmBookingSummary_RedirectsToIndex_WhenModelNull() {
        var sut = CreateControllerWithTempData(TestHelper.CreateInMemoryContext());

        var result = sut.ConfirmBookingSummary(null!);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public void ConfirmBookingSummary_ReturnsView_WhenModelStateInvalid() {
        var sut = CreateControllerWithTempData(TestHelper.CreateInMemoryContext());
        sut.ModelState.AddModelError("Test", "Invalid");

        var bookingSummary = TestHelper.CreateValidBookingSummaryVM([1, 2]);

        var result = sut.ConfirmBookingSummary(bookingSummary);

        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("BookingSummary");
    }

    [Fact]
    public void ConfirmBookingSummary_SetsTempDataAndRedirects_WhenModelValid() {
        var sut = CreateControllerWithTempData(TestHelper.CreateInMemoryContext());

        var bookingSummary = TestHelper.CreateValidBookingSummaryVM([1]);

        var result = sut.ConfirmBookingSummary(bookingSummary);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Confirmation));

        sut.TempData.ContainsKey("Confirmation").ShouldBeTrue();
        string? json = sut.TempData["Confirmation"] as string;
        json.ShouldNotBeNull();

        var confirmation = JsonSerializer.Deserialize<ConfirmationVM>(json!);
        confirmation!.CustomerEmail.ShouldBe(bookingSummary.CustomerEmail);
        confirmation.TourDate.ShouldBe(bookingSummary.TourDate);
    }

    [Fact]
    public void Confirmation_RedirectsToIndex_WhenNoTempData() {
        var sut = CreateControllerWithTempData(TestHelper.CreateInMemoryContext());

        var result = sut.Confirmation();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.Index));
    }

    [Fact]
    public void Confirmation_ReturnsView_WhenTempDataPresent() {
        var sut = CreateControllerWithTempData(TestHelper.CreateInMemoryContext());
        var confirmation = new ConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2]
        };
        sut.TempData["Confirmation"] = JsonSerializer.Serialize(confirmation);

        var result = sut.Confirmation();

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<ConfirmationVM>();
        model.CustomerEmail.ShouldBe(confirmation.CustomerEmail);
    }

    [Fact]
    public async Task SaveConfirmedBooking_ReturnsBadRequest_WhenBookingInvalidAsync() {
        var sut = CreateController();

        var result = await sut.SaveConfirmedBooking();

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid request");
    }

    [Fact]
    public async Task SaveConfirmedBooking_ReturnsBadRequest_WhenInvalidSuppliersAsync() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateControllerWithTempData(context);
        var confirmation = new ConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2]
        };

        var result = await sut.SaveConfirmedBooking(confirmation);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid supplier selection");
    }

    [Fact]
    public async Task SaveConfirmedBooking_SavesBookingAndReturnsOkAsync() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateControllerWithTempData(context);
        var confirmation = new ConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };
        sut.TempData["Confirmation"] = JsonSerializer.Serialize(confirmation);

        var result = await sut.SaveConfirmedBooking(confirmation);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        okResult.Value.ShouldNotBeNull();
        okResult.Value.ToString()!.ShouldContain("True");

        var bookings = context.Bookings.Include(b => b.BookingSuppliers).ToList();
        bookings.Count.ShouldBe(1);

        var booking = bookings[0];
        booking.CustomerEmail.ShouldBe(confirmation.CustomerEmail);
        booking.PurchaseCode.ShouldBe(confirmation.PurchaseCode);
        booking.TourDate.ShouldBe(confirmation.TourDate);
        booking.BookingSuppliers.Count.ShouldBe(5);
        booking.BookingSuppliers.Select(bs => bs.SupplierId).ShouldBe(confirmation.SelectedSupplierIds);
    }

    [Fact]
    public async Task SaveConfirmedBooking_ReturnsServerError_WhenDatabaseSaveThrowsAsync() {
        using var context = TestHelper.CreateContextWithSuppliersFailingOnSave();
        var sut = CreateControllerWithTempData(context);

        var confirmation = new ConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.SaveConfirmedBooking(confirmation);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(500);
        objectResult.Value.ShouldNotBeNull();
        objectResult.Value.ToString()!.ShouldContain("BookingSummary");
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
        return await PostIndex(tourDate, selectedSuppliers, contextSupplierCount, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);
    }
    private static async Task<IActionResult> PostIndex(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount, string customerEmail, string purchaseCode) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(contextSupplierCount);
        var sut = new HomeController(context);
        return await sut.Index(tourDate, selectedSuppliers, customerEmail, purchaseCode);
    }

    private static async Task<(IActionResult result, HomeController controller)> PostIndexWithController(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount) {
        return await PostIndexWithController(tourDate, selectedSuppliers, contextSupplierCount, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);
    }
    private static async Task<(IActionResult result, HomeController controller)> PostIndexWithController(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount, string customerEmail, string purchaseCode) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(contextSupplierCount);
        var sut = new HomeController(context);
        var result = await sut.Index(tourDate, selectedSuppliers, customerEmail, purchaseCode);
        return (result, sut);
    }

    private static HomeController CreateController() {
        return new HomeController(TestHelper.CreateInMemoryContext());
    }

    private static HomeController CreateControllerWithTempData(FoodTourContext context) {
        return new HomeController(context) {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }
}
