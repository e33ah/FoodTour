namespace FoodTour.Test.ServicesTest;

public class PurchaseCodeValidationServiceTests {
    private readonly FoodTourContext context = TestHelper.CreateInMemoryContext();
    private readonly PurchaseCodeValidationService sut;

    public PurchaseCodeValidationServiceTests() {
        sut = new PurchaseCodeValidationService(context);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_WhenPurchaseCodeIsNull() {
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = null!,
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("Purchase code is required.");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_WhenPurchaseCodeIsEmpty() {
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "",
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("Purchase code is required.");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_WhenPurchaseCodeIsWhitespace() {
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "   ",
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("Purchase code is required.");
    }

    [Theory]
    [InlineData((PurchaseCodeValidationType)999)]
    [InlineData((PurchaseCodeValidationType)(-1))]
    public async Task ValidateAsync_ReturnsFailure_WhenValidationTypeIsUnknown(PurchaseCodeValidationType validationType) {
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "TEST123",
            ValidationType = validationType
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("Unknown validation type.");
    }

    [Fact]
    public async Task ValidateAsync_MustExistAndBeActive_ReturnsSuccess_WhenCodeExistsAndIsActive() {
        await context.PurchaseCodes.AddAsync(new PurchaseCode { Code = "ACTIVE123", IsActive = true });
        await context.SaveChangesAsync();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "ACTIVE123",
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(true);
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_MustExistAndBeActive_ReturnsFailure_WhenCodeExistsButInactive() {
        await context.PurchaseCodes.AddAsync(new PurchaseCode { Code = "INACTIVE123", IsActive = false });
        await context.SaveChangesAsync();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "INACTIVE123",
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("Please enter a valid purchase code.");
    }

    [Fact]
    public async Task ValidateAsync_MustExistAndBeActive_ReturnsFailure_WhenCodeDoesNotExist() {
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "NONEXISTENT123",
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("Please enter a valid purchase code.");
    }

    [Fact]
    public async Task ValidateAsync_MustNotExist_ReturnsSuccess_WhenCodeDoesNotExistInBookings() {
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "NEWCODE123",
            ValidationType = PurchaseCodeValidationType.MustNotExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(true);
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_MustNotExist_ReturnsFailure_WhenCodeAlreadyExistsInBookings() {
        await context.Bookings.AddAsync(new Booking {
            PurchaseCode = "EXISTING123",
            CustomerEmail = "test@example.com",
            TourDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "EXISTING123",
            ValidationType = PurchaseCodeValidationType.MustNotExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("This purchase code has already been used.");
    }

    [Fact]
    public async Task ValidateAsync_MustExist_ReturnsSuccess_WhenCodeExistsInBookings() {
        await context.Bookings.AddAsync(new Booking {
            PurchaseCode = "BOOKED123",
            CustomerEmail = "test@example.com",
            TourDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "BOOKED123",
            ValidationType = PurchaseCodeValidationType.MustExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(true);
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_MustExist_ReturnsFailure_WhenCodeDoesNotExistInBookings() {
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "NOTBOOKED123",
            ValidationType = PurchaseCodeValidationType.MustExist
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("Booking not found for the provided purchase code.");
    }

    [Fact]
    public async Task ValidateAsync_MustExistForCancellation_ReturnsSuccess_WhenCodeExistsAndNotCancelled() {
        await context.Bookings.AddAsync(new Booking {
            PurchaseCode = "CANCEL123",
            CustomerEmail = "test@example.com",
            TourDate = DateTime.UtcNow.AddDays(1),
            IsCancelled = false
        });
        await context.SaveChangesAsync();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "CANCEL123",
            ValidationType = PurchaseCodeValidationType.MustExistForCancellation
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(true);
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_MustExistForCancellation_ReturnsFailure_WhenCodeDoesNotExist() {
        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "NOCANCEL123",
            ValidationType = PurchaseCodeValidationType.MustExistForCancellation
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("Booking not found for the provided purchase code.");
    }

    [Fact]
    public async Task ValidateAsync_MustExistForCancellation_ReturnsFailure_WhenBookingAlreadyCancelled() {
        await context.Bookings.AddAsync(new Booking {
            PurchaseCode = "CANCELLED123",
            CustomerEmail = "test@example.com",
            TourDate = DateTime.UtcNow.AddDays(1),
            IsCancelled = true
        });
        await context.SaveChangesAsync();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "CANCELLED123",
            ValidationType = PurchaseCodeValidationType.MustExistForCancellation
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(false);
        result.ErrorMessage.ShouldBe("This booking has already been cancelled.");
    }

    [Fact]
    public async Task ValidateAsync_HandlesMultipleConcurrentRequests_Correctly() {
        // Add test data
        await context.PurchaseCodes.AddAsync(new PurchaseCode { Code = "CONCURRENT1", IsActive = true });
        await context.Bookings.AddAsync(new Booking {
            PurchaseCode = "CONCURRENT2",
            CustomerEmail = "test@example.com",
            TourDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        var requests = new List<PurchaseCodeValidationRequest> {
            new() { PurchaseCode = "CONCURRENT1", ValidationType = PurchaseCodeValidationType.MustExistAndBeActive },
            new() { PurchaseCode = "CONCURRENT2", ValidationType = PurchaseCodeValidationType.MustExist },
            new() { PurchaseCode = "NEWCODE", ValidationType = PurchaseCodeValidationType.MustNotExist }
        };

        var tasks = requests.Select(r => sut.ValidateAsync(r));
        var results = await Task.WhenAll(tasks);

        results.ShouldAllBe(r => r.IsValid);
    }

    [Theory]
    [InlineData("CODE123")]
    [InlineData("PURCHASE-CODE-456")]
    [InlineData("TEST_789")]
    [InlineData("ABC")]
    [InlineData("1234567890")]
    public async Task ValidateAsync_HandlesVariousCodeFormats_Correctly(string purchaseCode) {
        await context.PurchaseCodes.AddAsync(new PurchaseCode { Code = purchaseCode, IsActive = true });
        await context.SaveChangesAsync();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = purchaseCode,
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        var result = await sut.ValidateAsync(request);

        result.IsValid.ShouldBe(true);
        result.ErrorMessage.ShouldBeNull();
    }
}