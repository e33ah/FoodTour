namespace FoodTour.Test.Controllers;

public class VisitsControllerTests : TestBase {
    private const string SUPPLIER_CODE = "TEST123";

    [Fact]
    public async Task RecordVisit_ReturnsOk_WhenSupplierIdIsValidAsync() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 2));

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

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, 999, supplier.ConfirmationCode, 2));

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

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, "WRONG_CODE", 2));

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

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(Guid.NewGuid(), supplier.Id, supplier.ConfirmationCode, 2));

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

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 2));

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

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 2));

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

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 2));

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

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(qrToken, supplierId, SUPPLIER_CODE, 2));

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
    public async Task RecordVisit_ReturnsBadRequest_WhenParametersAreInvalid(string qrTokenString, int selectedSupplierId, string? supplierCode) {
        var qrToken = Guid.Parse(qrTokenString);
        var sut = TestHelper.CreateVisitsController(TestHelper.CreateInMemoryContext());

        // null-coalescing operator is used to satisfy the non-nullable parameter, but the test will pass null to trigger the validation error
        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(qrToken, selectedSupplierId, supplierCode!, 2));

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

        await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 2));

        context.ChangeTracker.Clear();
        var updatedVisit = await context.Visits.FirstOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == supplier.Id, CancellationToken);
        updatedVisit.ShouldNotBeNull();
        updatedVisit.IsClaimed.ShouldBeTrue();
        updatedVisit.ClaimedAt.ShouldNotBeNull();
        updatedVisit.ClaimedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeClaim);
        updatedVisit.ClaimedAt.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenClaimedExceedsTotalGuests() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(1);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        booking.TotalGuests = 3;
        var supplier = context.Suppliers.First();
        supplier.ConfirmationCode = SUPPLIER_CODE;
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 4));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        object response = badRequest.Value.ShouldNotBeNull();
        string? errorMessage = response.GetType().GetProperty("error")?.GetValue(response) as string;
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("Cannot claim 4 tasting experiences");
        errorMessage.ShouldContain("This booking has only 3 guest(s)");
    }

    [Fact]
    public async Task RecordVisit_AllowsMultipleSuppliersToClaimFullGuestCount() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(2);
        var booking = TestHelper.CreateBookingWithVisits(context, 1);
        booking.TotalGuests = 5;

        var supplier1 = context.Suppliers.First();
        supplier1.ConfirmationCode = SUPPLIER_CODE;

        var supplier2 = context.Suppliers.Skip(1).First();
        supplier2.ConfirmationCode = SUPPLIER_CODE;

        var visit2 = new Visit { BookingId = booking.Id, SupplierId = supplier2.Id, IsClaimed = false };
        context.Visits.Add(visit2);
        context.SaveChanges();
        var sut = TestHelper.CreateVisitsController(context);

        await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier1.Id, supplier1.ConfirmationCode, 5));
        context.ChangeTracker.Clear();

        var result = await sut.RecordVisit(new VisitsController.RecordVisitDTO(booking.QrToken, supplier2.Id, supplier2.ConfirmationCode, 5));

        result.ShouldBeOfType<OkObjectResult>();
        context.ChangeTracker.Clear();
        var visit1 = await context.Visits.FirstAsync(v => v.SupplierId == supplier1.Id && v.BookingId == booking.Id, CancellationToken);
        var visit2Updated = await context.Visits.FirstAsync(v => v.SupplierId == supplier2.Id && v.BookingId == booking.Id, CancellationToken);
        visit1.TastingExperiencesClaimed.ShouldBe(5);
        visit2Updated.TastingExperiencesClaimed.ShouldBe(5);
    }
}

