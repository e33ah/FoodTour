namespace FoodTour.Test.Controllers;

public class HomeControllerIndexPostTests : HomeControllerTestBase {
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

    private static async Task<IActionResult> PostIndex(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount) {
        return await PostIndex(tourDate, selectedSuppliers, contextSupplierCount, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);
    }

    private static async Task<IActionResult> PostIndex(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount, string customerEmail, string purchaseCode) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(contextSupplierCount);
        var emailSender = Mock.Of<IEmailSender>();
        var bookingEmailService = new BookingEmailService();
        var sut = new HomeController(context, emailSender, bookingEmailService);
        return await sut.Index(tourDate, selectedSuppliers, customerEmail, purchaseCode);
    }

    private static async Task<(IActionResult result, HomeController controller)> PostIndexWithController(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount) {
        return await PostIndexWithController(tourDate, selectedSuppliers, contextSupplierCount, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);
    }

    private static async Task<(IActionResult result, HomeController controller)> PostIndexWithController(DateTime? tourDate, List<int> selectedSuppliers, int contextSupplierCount, string customerEmail, string purchaseCode) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(contextSupplierCount);
        var emailSender = Mock.Of<IEmailSender>();
        var bookingEmailService = new BookingEmailService();
        var sut = new HomeController(context, emailSender, bookingEmailService);
        var result = await sut.Index(tourDate, selectedSuppliers, customerEmail, purchaseCode);
        return (result, sut);
    }
}