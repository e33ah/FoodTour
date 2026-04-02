namespace FoodTour.Test.Services;

public class BookingValidationServiceTests {
    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenAllDataIsValid() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2),
            TestHelper.CreateSupplier(3),
            TestHelper.CreateSupplier(4),
            TestHelper.CreateSupplier(5)
        ]);
        await context.SaveChangesAsync();
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.IsTourDateValid.ShouldBeTrue();
        result.IsCustomerEmailValid.ShouldBeTrue();
        result.IsCustomerCountValid.ShouldBeTrue();
        result.AreSelectedSuppliersValid.ShouldBeTrue();
        // Purchase code validation is now handled separately by IPurchaseCodeValidationService
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenTourDateIsNull() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = null,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.IsTourDateValid.ShouldBeFalse();
        result.IsTourDateNull.ShouldBeTrue();
        result.ErrorMessages.ShouldContain("Tour date is required");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenTourDateIsInThePast() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);
        var pastDate = TestHelper.validTourDate.AddYears(-2);

        var request = new BookingValidationRequest {
            TourDate = pastDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.IsTourDateValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateAsync_ReturnsInvalid_WhenEmailIsMissing(string? customerEmail) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = customerEmail,
            CustomerCount = 2,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };


        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.IsCustomerEmailValid.ShouldBeFalse();
        result.IsCustomerEmailEmpty.ShouldBeTrue();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing_at_sign.com")]
    [InlineData("missing_domain@")]
    [InlineData("@missing-local-part.com")]
    public async Task ValidateAsync_ReturnsInvalid_WhenEmailIsInvalidFormat(string email) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = email,
            CustomerCount = 2,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.IsCustomerEmailValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_ReturnsInvalid_WhenCustomerCountIsInvalid(int customerCount) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = customerCount,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.IsCustomerCountValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenCustomerCountIsNull() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2),
            TestHelper.CreateSupplier(3),
            TestHelper.CreateSupplier(4),
            TestHelper.CreateSupplier(5)
        ]);
        await context.SaveChangesAsync();
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = null, // Optional field
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.IsCustomerCountValid.ShouldBeTrue(); // Should be true when not provided
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenNoSuppliersSelected() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = []
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.AreSelectedSuppliersValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenTooManySuppliersSelected() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = [1, 2, 3, 4, 5, 6] // More than max allowed
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.AreSelectedSuppliersValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task ValidateAsync_ReturnsInvalid_WhenFewerThanRequiredSuppliersSelected(int supplierCount) {
        using var context = TestHelper.CreateInMemoryContext();
        var suppliers = Enumerable.Range(1, supplierCount)
            .Select(i => TestHelper.CreateSupplier(i))
            .ToArray();
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();
        var sut = new BookingValidationService(context);

        var selectedSupplierIds = Enumerable.Range(1, supplierCount).ToList();
        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = selectedSupplierIds
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.AreSelectedSuppliersValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenExactlyRequiredSuppliersSelected() {
        using var context = TestHelper.CreateInMemoryContext();
        var suppliers = Enumerable.Range(1, TestHelper.EXPECTED_MAX_SELECTIONS)
            .Select(i => TestHelper.CreateSupplier(i))
            .ToArray();
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();
        var sut = new BookingValidationService(context);

        var selectedSupplierIds = Enumerable.Range(1, TestHelper.EXPECTED_MAX_SELECTIONS).ToList();
        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = selectedSupplierIds
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.AreSelectedSuppliersValid.ShouldBeTrue();
    }

    [Fact]
    public void IsTourDateValid_ReturnsTrue_WhenDateIsValid() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsTourDateValid(TestHelper.validTourDate);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTourDateValid_ReturnsFalse_WhenDateIsNull() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsTourDateValid(null);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCustomerEmailValid_ReturnsTrue_WhenEmailIsValid() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsCustomerEmailValid(TestHelper.VALID_EMAIL);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData(null)]
    public void IsCustomerEmailValid_ReturnsFalse_WhenEmailIsInvalid(string? email) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsCustomerEmailValid(email);

        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void IsCustomerCountValid_ReturnsTrue_WhenCountIsPositive(int count) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsCustomerCountValid(count);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(null)]
    public void IsCustomerCountValid_ReturnsFalse_WhenCountIsZeroOrNegativeOrNull(int? count) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsCustomerCountValid(count);

        result.ShouldBeFalse();
    }
}
