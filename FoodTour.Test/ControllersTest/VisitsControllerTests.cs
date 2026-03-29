namespace FoodTour.Test.Controllers;

public class VisitsControllerTests {
    private const string SUPPLIER_CODE = "TEST123";

    [Fact]
    public async Task RecordVisit_ReturnsOk_WhenSupplierIdIsValidAsync() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode));

        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenSupplierNotFoundAsync() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, 999, supplier.ConfirmationCode));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("Business not found in Food Tour. Please contact Support.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequestWithSpecificMessage_WhenSupplierCodeIsIncorrect() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, "WRONG_CODE"));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("Invalid business code provided. Please try again or contact Support.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenBookingNotFound() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(Guid.NewGuid(), supplier.Id, supplier.ConfirmationCode));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("Booking not found. Please contact Support.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenBookingIsCancelled() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        booking.IsCancelled = true;
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("Booking not found. Please contact Support.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenVisitNotFound() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(2);
        var booking = TestHelper.CreateBookingWithVisits(context, 1); // Only creates visit for supplier 1
        var supplier = context.Suppliers.Skip(1).First(); // Get supplier 2
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("Visit not found for this booking and business. Please contact Support.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenVisitAlreadyClaimed() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        var visit = context.Visits.First();
        visit.IsClaimed = true;
        visit.ClaimedAt = DateTime.UtcNow;
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("This visit has already been confirmed.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task RecordVisit_ReturnsBadRequest_WhenSupplierIdIsInvalid(int supplierId) {
        var sut = TestHelper.CreateVisitsController(TestHelper.CreateInMemoryContext());
        var qrToken = Guid.NewGuid();

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(qrToken, supplierId, SUPPLIER_CODE));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("Invalid booking or business information.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenPayloadIsNull() {
        var sut = TestHelper.CreateVisitsController(TestHelper.CreateInMemoryContext());

        var result = await sut.RecordVisit(null);

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("Invalid booking or business information.");
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 1, SUPPLIER_CODE)]
    [InlineData("e4f50c60-4d42-11ec-81d3-0242ac130003", 1, "")]
    [InlineData("e4f50c60-4d42-11ec-81d3-0242ac130003", 1, null)]
    public async Task RecordVisit_ReturnsBadRequest_WhenParametersAreInvalid(string qrTokenString, int selectedSupplierId, string supplierCode) {
        var qrToken = Guid.Parse(qrTokenString);
        var sut = TestHelper.CreateVisitsController(TestHelper.CreateInMemoryContext());

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(qrToken, selectedSupplierId, supplierCode));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        response.GetType().GetProperty("error")?.GetValue(response).ShouldBe("Invalid booking or business information.");
    }

    [Fact]
    public async Task RecordVisit_SetsClaimedAtTimestamp_WhenVisitRecordedSuccessfully() {
        var beforeClaim = DateTime.UtcNow.AddMilliseconds(-10); // 10ms buffer
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode));

        context.ChangeTracker.Clear();
        var updatedVisit = await context.Visits.FirstOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == supplier.Id);
        updatedVisit.ShouldNotBeNull();
        updatedVisit.IsClaimed.ShouldBeTrue();
        updatedVisit.ClaimedAt.ShouldNotBeNull();
        updatedVisit.ClaimedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeClaim);
        updatedVisit.ClaimedAt.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }
}