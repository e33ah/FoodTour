namespace FoodTour.Test.Controllers;

public class HomeControllerRecordVisitTests : HomeControllerTestBase {
    [Fact]
    public async Task RecordVisit_ReturnsOk_WhenSupplierIdIsValidAsync() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        booking.Id = 1;
        var visit = new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false };
        context.Visits.Add(visit);
        context.SaveChanges();
        var sut = new HomeController(context);

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, supplier.Id));

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        var updatedVisit = await context.Visits.FirstOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == supplier.Id);
        updatedVisit.ShouldNotBeNull();
        updatedVisit.IsClaimed.ShouldBeTrue();
        updatedVisit.ClaimedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenVisitNotFoundAsync() {
        var supplier = TestHelper.CreateSupplier(1, "Tasty Treats", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = new HomeController(context);

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, 999));

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
        var sut = new HomeController(context);

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, supplier.Id));

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

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(qrToken, supplierId));

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid booking or supplier information");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenBookingNotFound() {
        var sut = new HomeController(TestHelper.CreateInMemoryContext());
        var qrToken = Guid.NewGuid();

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(qrToken, 1));

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Booking not found.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenQrTokenIsEmpty() {
        var sut = CreateController();

        var result = await sut.RecordVisit(new HomeController.RecordVisitDTO(Guid.Empty, 1));

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid booking or supplier information");
    }

    [Fact]
    public async Task RecordVisit_ReturnsError_WhenBookingIsCancelled() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var cancelledBooking = TestHelper.CreateValidBooking([1]);
        cancelledBooking.IsCancelled = true;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [cancelledBooking]);
        var sut = new HomeController(context);

        var payload = new HomeController.RecordVisitDTO(cancelledBooking.QrToken, 1);

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
        var sut = new HomeController(context);
        var beforeClaim = DateTime.UtcNow;

        await sut.RecordVisit(new HomeController.RecordVisitDTO(booking.QrToken, supplier.Id));

        var updatedVisit = await context.Visits.FirstOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == supplier.Id);
        updatedVisit.ShouldNotBeNull();
        updatedVisit.ClaimedAt.ShouldNotBeNull();
        updatedVisit.ClaimedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeClaim);
        updatedVisit.ClaimedAt.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

}