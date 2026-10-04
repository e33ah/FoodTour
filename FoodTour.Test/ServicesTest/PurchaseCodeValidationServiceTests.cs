namespace FoodTour.Test.Services;

public class PurchaseCodeValidationServiceTests : TestBase {
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_ReturnsFailure_WhenPurchaseCodeIsBlank(string purchaseCode) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = purchaseCode,
            ValidationType = PurchaseCodeValidationType.MustExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Purchase code is required.");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsSuccess_WhenCodeMustNotExistAndBookingIsMissing() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = TestHelper.VALID_CODE,
            ValidationType = PurchaseCodeValidationType.MustNotExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_WhenCodeMustNotExistAndBookingExists() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Bookings.Add(TestHelper.CreateValidBooking([]));
        await context.SaveChangesAsync(CancellationToken);
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = TestHelper.VALID_CODE,
            ValidationType = PurchaseCodeValidationType.MustNotExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("This purchase code has already been used.");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsSuccess_WhenCodeMustExistAndBookingExists() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Bookings.Add(TestHelper.CreateValidBooking([]));
        await context.SaveChangesAsync(CancellationToken);
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = TestHelper.VALID_CODE,
            ValidationType = PurchaseCodeValidationType.MustExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_WhenCodeMustExistAndBookingIsMissing() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = TestHelper.VALID_CODE,
            ValidationType = PurchaseCodeValidationType.MustExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Booking not found for the provided purchase code.");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsSuccess_WhenCancellationCodeExistsAndBookingIsActive() {
        using var context = TestHelper.CreateInMemoryContext();
        context.Bookings.Add(TestHelper.CreateValidBooking([]));
        await context.SaveChangesAsync(CancellationToken);
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = TestHelper.VALID_CODE,
            ValidationType = PurchaseCodeValidationType.MustExistForCancellation
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_WhenCancellationCodeExistsAndBookingIsCancelled() {
        using var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        booking.IsCancelled = true;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = TestHelper.VALID_CODE,
            ValidationType = PurchaseCodeValidationType.MustExistForCancellation
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("This booking has already been cancelled.");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_WhenCancellationCodeDoesNotExist() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = TestHelper.VALID_CODE,
            ValidationType = PurchaseCodeValidationType.MustExistForCancellation
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Booking not found for the provided purchase code.");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_WhenValidationTypeIsUnknown() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = new PurchaseCodeValidationService(context);
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = TestHelper.VALID_CODE,
            ValidationType = (PurchaseCodeValidationType)int.MaxValue
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Unknown validation type.");
    }
}
