namespace FoodTour.Test.Controllers;

public class HomeControllerRecordVisitTests : HomeControllerTestBase {
    private const string SUPPLIER_CODE = "CODE";
    private const string INVALID_CODE = "WRONGCODE";

    [Fact]
    public async Task RecordVisit_ReturnsOk_WhenSupplierIdIsValidAsync() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        booking.Id = 1;
        var visit = new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false };
        context.Visits.Add(visit);
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode));

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        var updatedVisit = await context.Visits.FirstOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == supplier.Id);
        updatedVisit.ShouldNotBeNull();
        updatedVisit.IsClaimed.ShouldBeTrue();
        updatedVisit.ClaimedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenSupplierNotFoundAsync() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, 999, supplier.ConfirmationCode));

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Business not found");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenVisitNotFoundAsync() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, 1, supplier.ConfirmationCode));

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Visit not found");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenVisitAlreadyClaimedAsync() {
        var supplier = TestHelper.CreateSupplier(1, "Already Visited", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var visit = new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = true, ClaimedAt = DateTime.UtcNow };
        context.Visits.Add(visit);
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode));

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("already been confirmed");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task RecordVisit_ReturnsBadRequest_SupplierIdIsInvalid(int supplierId) {
        var sut = CreateController();
        var qrToken = Guid.NewGuid();

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(qrToken, supplierId, SUPPLIER_CODE));

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid booking or business information");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenBookingNotFound() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());
        var qrToken = Guid.NewGuid();

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(qrToken, 1, SUPPLIER_CODE));

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Booking not found.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsError_WhenBookingIsCancelled() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var cancelledBooking = TestHelper.CreateValidBooking([1]);
        cancelledBooking.IsCancelled = true;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [cancelledBooking]);
        var sut = CreateConfiguredController(context);

        var payload = new HomeController.RecordVisitDTO(cancelledBooking.QrToken, 1, supplier.ConfirmationCode);

        var result = await sut.RecordVisit(payload);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Booking not found.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenPayloadIsNull() {
        var sut = CreateController();

        var result = await sut.RecordVisit(null);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task RecordVisit_SetsClaimedAtTimestamp_WhenVisitRecordedSuccessfully() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var visit = new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false };
        context.Visits.Add(visit);
        context.SaveChanges();
        var sut = CreateConfiguredController(context);
        var beforeClaim = DateTime.UtcNow;

        await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode));

        var updatedVisit = await context.Visits.FirstOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == supplier.Id);
        updatedVisit.ShouldNotBeNull();
        updatedVisit.ClaimedAt.ShouldNotBeNull();
        updatedVisit.ClaimedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeClaim);
        updatedVisit.ClaimedAt.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenSupplierConfirmationCodeIsIncorrect() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);
        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, supplier.Id, INVALID_CODE));
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid business code provided");
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 1, SUPPLIER_CODE)]
    [InlineData("e4f50c60-4d42-11ec-81d3-0242ac130003", 0, SUPPLIER_CODE)]
    [InlineData("e4f50c60-4d42-11ec-81d3-0242ac130003", 1, "")]
    public async Task RecordVisit_ReturnsBadRequest_WhenParametersAreInvalid(Guid qrToken, int selectedSupplierId, string supplierCode) {
        var sut = CreateController();
        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(qrToken, selectedSupplierId, supplierCode));
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid booking or business information");
    }
}