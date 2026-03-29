namespace FoodTour.Test.Controllers;

public class HomeControllerValidationTests : HomeControllerTestBase {

    [Theory]
    [InlineData(null)]
    public async Task Index_AddsModelError_WhenTourDateIsNull(DateTime? tourDate) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(tourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.TourDate)]!.Errors[0].ErrorMessage.ShouldBe("Please select a date for your tour.");
    }

    [Fact]
    public async Task Index_AddsModelError_WhenTourDateIsInPast() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);
        var pastDate = DateTime.Today.AddDays(-1);

        var result = await sut.Index(pastDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.TourDate)]!.Errors[0].ErrorMessage.ShouldBe("Tours are only available Monday to Saturday between April and October, and only for future dates.");
    }

    [Fact]
    public async Task Index_AddsModelError_WhenTourDateIsOnSunday() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);
        
        // Find next Sunday after today
        var sunday = DateTime.Today.AddDays(1);
        while (sunday.DayOfWeek != DayOfWeek.Sunday) {
            sunday = sunday.AddDays(1);
        }

        var result = await sut.Index(sunday, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.TourDate)]!.Errors[0].ErrorMessage.ShouldBe("Tours are only available Monday to Saturday between April and October, and only for future dates.");
    }

    [Theory]
    [InlineData(3, 15)] // March - too early
    [InlineData(11, 15)] // November - too late
    public async Task Index_AddsModelError_WhenTourDateIsOutsideSeasonRange(int month, int day) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);
        var outOfSeasonDate = new DateTime(TestHelper.nextYear, month, day);

        var result = await sut.Index(outOfSeasonDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.TourDate)]!.Errors[0].ErrorMessage.ShouldBe("Tours are only available Monday to Saturday between April and October, and only for future dates.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Index_AddsModelError_WhenCustomerEmailIsEmpty(string? customerEmail) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        // null-coalescing operator is used to satisfy the non-nullable parameter, but the test will pass null to trigger the validation error
        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, customerEmail!, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.CustomerEmail)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.CustomerEmail)]!.Errors[0].ErrorMessage.ShouldBe("Please enter your email address.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public async Task Index_AddsModelError_WhenCustomerEmailIsInvalid(string invalidEmail) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, invalidEmail, invalidEmail, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.CustomerEmail)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.CustomerEmail)]!.Errors[0].ErrorMessage.ShouldBe("Please enter a valid email address.");
    }

    [Fact]
    public async Task Index_AddsModelError_WhenEmailAddressesDoNotMatch() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, "different@email.com", TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.CustomerEmailConfirmation)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.CustomerEmailConfirmation)]!.Errors[0].ErrorMessage.ShouldBe("The email addresses do not match.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Index_AddsModelError_WhenPurchaseCodeIsEmpty(string? purchaseCode) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        // null-coalescing operator is used to satisfy the non-nullable parameter, but the test will pass null to trigger the validation error
        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, purchaseCode!);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.PurchaseCode)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.PurchaseCode)]!.Errors[0].ErrorMessage.ShouldBe("Please enter your purchase code.");
    }

    [Fact]
    public async Task Index_AddsModelError_WhenPurchaseCodeIsInvalid() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = "VALIDCODE", IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, "INVALIDCODE");

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.PurchaseCode)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.PurchaseCode)]!.Errors[0].ErrorMessage.ShouldBe("Please enter a valid purchase code.");
    }

    [Fact]
    public async Task Index_AddsModelError_WhenPurchaseCodeIsInactive() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = false });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(nameof(FoodTourVM.PurchaseCode)).ShouldBeTrue();
        sut.ModelState[nameof(FoodTourVM.PurchaseCode)]!.Errors[0].ErrorMessage.ShouldBe("Please enter a valid purchase code.");
    }

    [Theory]
    [InlineData(4)] // Too few
    [InlineData(6)] // Too many
    public async Task Index_AddsModelError_WhenIncorrectNumberOfSuppliers(int supplierCount) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(6);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(supplierCount);

        var result = await sut.Index(TestHelper.validTourDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldBe("You must select 5 tasting experiences for your tour.");
    }

    [Theory]
    [InlineData(4, 15)] // April - valid
    [InlineData(6, 21)] // June - valid (Monday)  
    [InlineData(10, 30)] // October - valid
    public async Task Index_AcceptsValidDatesWithinSeason(int month, int day) {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);
        var validDate = new DateTime(TestHelper.nextYear, month, day);
        var sut = CreateConfiguredController(context);

        var result = await sut.Index(validDate, selectedSuppliers, TestHelper.VALID_EMAIL, TestHelper.VALID_EMAIL, TestHelper.VALID_CODE);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.BookingSummary));
    }
}