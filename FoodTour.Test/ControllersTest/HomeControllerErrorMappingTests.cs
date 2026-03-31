namespace FoodTour.Test.Controllers;

public class HomeControllerErrorMappingTests : HomeControllerTestBase {
    [Fact]
    public async Task Index_MapsValidationErrors_WhenMultipleValidationFailures() {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateHomeController(context);

        // Call with multiple invalid values to test error mapping
        var result = await controller.Index(
            tourDate: null, // Invalid
            selectedSuppliers: [1], // Invalid count
            customerEmail: "", // Invalid
            customerEmailConfirmation: "different@email.com", // Different
            purchaseCode: "" // Invalid
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.IsValid.ShouldBeFalse();
        
        // Verify specific error mappings
        controller.ModelState.ContainsKey(nameof(FoodTourVM.TourDate)).ShouldBeTrue();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.CustomerEmail)).ShouldBeTrue();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.CustomerEmailConfirmation)).ShouldBeTrue();
        controller.ModelState.ContainsKey(nameof(FoodTourVM.PurchaseCode)).ShouldBeTrue();
        controller.ModelState.ContainsKey(string.Empty).ShouldBeTrue(); // Supplier selection error
    }

    [Fact]
    public async Task Index_MapsSpecificTourDateError_WhenTourDateIsNull() {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateHomeController(context);

        var result = await controller.Index(
            tourDate: null,
            selectedSuppliers: [1, 2, 3, 4, 5],
            customerEmail: TestHelper.VALID_EMAIL,
            customerEmailConfirmation: TestHelper.VALID_EMAIL,
            purchaseCode: TestHelper.VALID_CODE
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(FoodTourVM.TourDate)]?.Errors[0].ErrorMessage
            .ShouldBe("Please select a date for your tour.");
    }

    [Fact]
    public async Task Index_MapsGenericTourDateError_WhenTourDateIsInvalid() {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateHomeController(context);
        var pastDate = DateTime.Today.AddDays(-1);

        var result = await controller.Index(
            tourDate: pastDate,
            selectedSuppliers: [1, 2, 3, 4, 5],
            customerEmail: TestHelper.VALID_EMAIL,
            customerEmailConfirmation: TestHelper.VALID_EMAIL,
            purchaseCode: TestHelper.VALID_CODE
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(FoodTourVM.TourDate)]?.Errors[0].ErrorMessage
            .ShouldBe("Tours are only available Monday to Saturday between April and October, and only for future dates.");
    }

    [Fact]
    public async Task Index_MapsSpecificEmailError_WhenEmailIsEmpty() {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateHomeController(context);

        var result = await controller.Index(
            tourDate: TestHelper.validTourDate,
            selectedSuppliers: [1, 2, 3, 4, 5],
            customerEmail: "",
            customerEmailConfirmation: "",
            purchaseCode: TestHelper.VALID_CODE
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(FoodTourVM.CustomerEmail)]?.Errors[0].ErrorMessage
            .ShouldBe("Please enter your email address.");
    }

    [Fact]
    public async Task Index_MapsGenericEmailError_WhenEmailIsInvalid() {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateHomeController(context);

        var result = await controller.Index(
            tourDate: TestHelper.validTourDate,
            selectedSuppliers: [1, 2, 3, 4, 5],
            customerEmail: "invalid-email",
            customerEmailConfirmation: "invalid-email",
            purchaseCode: TestHelper.VALID_CODE
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(FoodTourVM.CustomerEmail)]?.Errors[0].ErrorMessage
            .ShouldBe("Please enter a valid email address.");
    }

    [Fact]
    public async Task Index_MapsEmailConfirmationError_WhenEmailsDoNotMatch() {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateHomeController(context);

        var result = await controller.Index(
            tourDate: TestHelper.validTourDate,
            selectedSuppliers: [1, 2, 3, 4, 5],
            customerEmail: "test@example.com",
            customerEmailConfirmation: "different@example.com",
            purchaseCode: TestHelper.VALID_CODE
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(FoodTourVM.CustomerEmailConfirmation)]?.Errors[0].ErrorMessage
            .ShouldBe("The email addresses do not match.");
    }

    [Fact]
    public async Task Index_MapsSpecificPurchaseCodeError_WhenPurchaseCodeIsEmpty() {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateHomeController(context);

        var result = await controller.Index(
            tourDate: TestHelper.validTourDate,
            selectedSuppliers: [1, 2, 3, 4, 5],
            customerEmail: TestHelper.VALID_EMAIL,
            customerEmailConfirmation: TestHelper.VALID_EMAIL,
            purchaseCode: ""
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(FoodTourVM.PurchaseCode)]?.Errors[0].ErrorMessage
            .ShouldBe("Purchase code is required.");
    }

    [Fact]
    public async Task Index_MapsGenericPurchaseCodeError_WhenPurchaseCodeIsInvalid() {
        using var context = TestHelper.CreateInMemoryContext();
        context.PurchaseCodes.Add(new PurchaseCode { Code = "DIFFERENT_CODE", IsActive = true });
        await context.SaveChangesAsync();
        var controller = TestHelper.CreateHomeController(context);

        var result = await controller.Index(
            tourDate: TestHelper.validTourDate,
            selectedSuppliers: [1, 2, 3, 4, 5],
            customerEmail: TestHelper.VALID_EMAIL,
            customerEmailConfirmation: TestHelper.VALID_EMAIL,
            purchaseCode: "INVALID_CODE"
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(FoodTourVM.PurchaseCode)]?.Errors[0].ErrorMessage
            .ShouldBe("Please enter a valid purchase code.");
    }

    [Fact]
    public async Task Index_MapsSupplierSelectionError_WhenInvalidSupplierCount() {
        using var context = TestHelper.CreateInMemoryContext();
        context.PurchaseCodes.Add(new PurchaseCode { Code = TestHelper.VALID_CODE, IsActive = true });
        await context.SaveChangesAsync();
        var controller = TestHelper.CreateHomeController(context);

        var result = await controller.Index(
            tourDate: TestHelper.validTourDate,
            selectedSuppliers: [1, 2, 3], // Only 3 suppliers
            customerEmail: TestHelper.VALID_EMAIL,
            customerEmailConfirmation: TestHelper.VALID_EMAIL,
            purchaseCode: TestHelper.VALID_CODE
        );

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[string.Empty]?.Errors[0].ErrorMessage
            .ShouldBe("You must select 5 tasting experiences for your tour.");
    }
}