namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class VisitsControllerApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenPayloadIsInvalid() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        RecordVisitRequest request = new() {
            QrToken = booking.QrToken,
            SelectedSupplierId = 0,
            SupplierCode = string.Empty,
            TastingExperiencesClaimed = 0
        };

        var response = await PutRecordVisitAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var result = await ReadJsonAsync(response);
        result.GetProperty("error").GetString().ShouldBe("Invalid booking or business information.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenSupplierCodeIsWrong() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var visit = Fixture.SeedData.SupplierVisitBookingVisits[0];
        RecordVisitRequest request = new() {
            QrToken = booking.QrToken,
            SelectedSupplierId = visit.SupplierId,
            SupplierCode = "WRONG",
            TastingExperiencesClaimed = 1
        };

        var response = await PutRecordVisitAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var result = await ReadJsonAsync(response);
        result.GetProperty("error").GetString().ShouldBe("Invalid business code provided. Please try again or contact Support.");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenClaimedCountExceedsBookingTotal() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var visit = Fixture.SeedData.SupplierVisitBookingVisits[0];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == visit.SupplierId);
        RecordVisitRequest request = new() {
            QrToken = booking.QrToken,
            SelectedSupplierId = supplier.Id,
            SupplierCode = supplier.ConfirmationCode,
            TastingExperiencesClaimed = booking.TotalGuests + 1
        };

        var response = await PutRecordVisitAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var result = await ReadJsonAsync(response);
        string? error = result.GetProperty("error").GetString();
        error.ShouldNotBeNull();
        error.ShouldContain("Cannot claim");
    }

    [Fact]
    public async Task RecordVisit_ReturnsBadRequest_WhenVisitHasAlreadyBeenClaimed() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var visit = Fixture.SeedData.SupplierVisitBookingVisits[1];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == visit.SupplierId);

        await using (var context = Fixture.CreateDbContext()) {
            var existingVisit = await context.Visits.SingleAsync(
                currentVisit => currentVisit.BookingId == booking.Id && currentVisit.SupplierId == supplier.Id,
                TestContext.Current.CancellationToken);
            existingVisit.IsClaimed = true;
            existingVisit.ClaimedAt = DateTime.UtcNow;
            existingVisit.TastingExperiencesClaimed = 1;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        RecordVisitRequest request = new() {
            QrToken = booking.QrToken,
            SelectedSupplierId = supplier.Id,
            SupplierCode = supplier.ConfirmationCode,
            TastingExperiencesClaimed = 1
        };

        var response = await PutRecordVisitAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var result = await ReadJsonAsync(response);
        result.GetProperty("error").GetString().ShouldBe("This visit has already been confirmed.");
    }

    [Fact]
    public async Task RecordVisit_ClaimsVisitAndSendsNotifications_WhenRequestIsValid() {
        using var sut = CreateApiClient();
        Fixture.TestEmailSender.Clear();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var visit = Fixture.SeedData.SupplierVisitBookingVisits[2];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == visit.SupplierId);
        RecordVisitRequest request = new() {
            QrToken = booking.QrToken,
            SelectedSupplierId = supplier.Id,
            SupplierCode = supplier.ConfirmationCode,
            TastingExperiencesClaimed = booking.TotalGuests
        };

        var response = await PutRecordVisitAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await ReadJsonAsync(response);
        result.GetProperty("success").GetBoolean().ShouldBeTrue();
        result.GetProperty("tastingExperiencesClaimed").GetInt32().ShouldBe(booking.TotalGuests);
        result.GetProperty("totalGuests").GetInt32().ShouldBe(booking.TotalGuests);

        var updatedVisit = await GetVisitAsync(booking.Id, supplier.Id);
        updatedVisit.IsClaimed.ShouldBeTrue();
        updatedVisit.TastingExperiencesClaimed.ShouldBe(booking.TotalGuests);
        updatedVisit.ClaimedAt.ShouldNotBeNull();

        Fixture.TestEmailSender.SentEmails.Count.ShouldBeGreaterThanOrEqualTo(2);
        Fixture.TestEmailSender.SentEmails.Any(email => email.To == "admin@foodtour.com").ShouldBeTrue();
        Fixture.TestEmailSender.SentEmails.Any(email => email.To == supplier.ContactEmail).ShouldBeTrue();
    }
}
