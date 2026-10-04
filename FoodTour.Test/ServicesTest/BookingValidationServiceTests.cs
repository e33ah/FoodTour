namespace FoodTour.Test.Services;

public class BookingValidationServiceTests : TestBase {
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
        await context.SaveChangesAsync(CancellationToken);
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
        result.ErrorMessages.ShouldContain("Customer email is required");
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
        result.ErrorMessages.ShouldContain($"Customer email '{email}' is not in a valid format");
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
        result.ErrorMessages.ShouldContain($"Customer count must be greater than 0. Provided value: {customerCount}");
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
        await context.SaveChangesAsync(CancellationToken);
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
        result.ErrorMessages.ShouldContain("At least one supplier must be selected");
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
        result.ErrorMessages.ShouldContain("Exactly 5 suppliers must be selected. Currently selected: 6");
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
        await context.SaveChangesAsync(CancellationToken);
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
        result.ErrorMessages.ShouldContain($"Exactly 5 suppliers must be selected. Currently selected: {supplierCount}");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenExactlyRequiredSuppliersSelected() {
        using var context = TestHelper.CreateInMemoryContext();
        var suppliers = Enumerable.Range(1, TestHelper.EXPECTED_MAX_SELECTIONS)
            .Select(i => TestHelper.CreateSupplier(i))
            .ToArray();
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync(CancellationToken);
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
    public async Task ValidateAsync_ReturnsValid_WhenTastingCountSpecifiesRequiredSupplierCount() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2)
        ]);
        await context.SaveChangesAsync(CancellationToken);
        var sut = new BookingValidationService(context);
        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            TastingCount = 2,
            SelectedSupplierIds = [1, 2]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.AreSelectedSuppliersValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenSelectedSupplierIsMissingOrInactive() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2),
            TestHelper.CreateSupplier(3),
            TestHelper.CreateSupplier(4)
        ]);
        await context.SaveChangesAsync(CancellationToken);
        var sut = new BookingValidationService(context);
        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = [1, 2, 3, 4, 99]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.AreSelectedSuppliersValid.ShouldBeFalse();
        result.ErrorMessages.ShouldContain(
            "One or more selected suppliers do not exist or are inactive. Missing supplier IDs: [99]");
    }

    [Fact]
    public async Task ValidateAsync_ReportsCommaSeparatedMissingIds_WhenMultipleSuppliersAreMissing() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2),
            TestHelper.CreateSupplier(3)
        ]);
        await context.SaveChangesAsync(CancellationToken);
        var sut = new BookingValidationService(context);
        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            SelectedSupplierIds = [1, 2, 3, 98, 99]
        };

        var result = await sut.ValidateAsync(request);

        result.AreSelectedSuppliersValid.ShouldBeFalse();
        result.ErrorMessages.ShouldContain(
            "One or more selected suppliers do not exist or are inactive. Missing supplier IDs: [98, 99]");
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
    public void IsTourDateValid_ReturnsFalse_WhenDateIsToday() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsTourDateValid(DateTime.Today);

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

    [Theory]
    [InlineData("John Smith")]
    [InlineData("Mary-Jane O'Connor")]
    [InlineData("José García")]
    [InlineData("李明")]
    [InlineData("A")]
    public void IsCustomerNameValid_ReturnsTrue_WhenNameIsValid(string customerName) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsCustomerNameValid(customerName);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsCustomerNameValid_ReturnsFalse_WhenNameIsEmpty(string? customerName) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);

        bool result = sut.IsCustomerNameValid(customerName);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCustomerNameValid_ReturnsFalse_WhenNameIsTooLong() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);
        string longName = new('A', 1024);

        bool result = sut.IsCustomerNameValid(longName);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCustomerNameValid_ReturnsTrue_WhenNameIsJustUnderMaxLength() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new BookingValidationService(context);
        string longName = new('A', 1023);

        bool result = sut.IsCustomerNameValid(longName);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenCustomerNameIsNotProvided() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2),
            TestHelper.CreateSupplier(3),
            TestHelper.CreateSupplier(4),
            TestHelper.CreateSupplier(5)
        ]);
        await context.SaveChangesAsync(CancellationToken);
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            CustomerName = null,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.IsCustomerNameValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenCustomerNameIsProvided() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2),
            TestHelper.CreateSupplier(3),
            TestHelper.CreateSupplier(4),
            TestHelper.CreateSupplier(5)
        ]);
        await context.SaveChangesAsync(CancellationToken);
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            CustomerName = "John Smith",
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.IsCustomerNameValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenCustomerNameIsEmptyString() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2),
            TestHelper.CreateSupplier(3),
            TestHelper.CreateSupplier(4),
            TestHelper.CreateSupplier(5)
        ]);
        await context.SaveChangesAsync(CancellationToken);
        var sut = new BookingValidationService(context);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            CustomerName = "",
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.IsCustomerNameValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenCustomerNameIsTooLong() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange([
            TestHelper.CreateSupplier(1),
            TestHelper.CreateSupplier(2),
            TestHelper.CreateSupplier(3),
            TestHelper.CreateSupplier(4),
            TestHelper.CreateSupplier(5)
        ]);
        await context.SaveChangesAsync(CancellationToken);
        var sut = new BookingValidationService(context);
        string longName = new('A', 1024);

        var request = new BookingValidationRequest {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            CustomerCount = 2,
            CustomerName = longName,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.IsCustomerNameValid.ShouldBeFalse();
    }
}
