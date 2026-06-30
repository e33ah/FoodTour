namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public sealed class SupplierVisitApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task SupplierVisit_when_supplier_code_is_invalid_returns_validation_error_and_preserves_visit_works() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var seededVisit = Fixture.SeedData.SupplierVisitBookingVisits[0];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == seededVisit.SupplierId);
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);

        var beforeVisit = await FindVisitAsync(booking.Id, supplier.Id);
        beforeVisit.ShouldNotBeNull();
        var beforeSnapshot = new VisitSnapshot(beforeVisit.IsClaimed, beforeVisit.ClaimedAt, beforeVisit.TastingExperiencesClaimed);

        using var request = CreateSupplierVisitPostRequest(
            booking.QrToken,
            supplier.Id,
            "WRONG-CODE",
            1,
            antiForgeryData);

        var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);
        string html = await ReadHtmlAsync(response, TestContext.Current.CancellationToken);
        var afterVisit = await FindVisitAsync(booking.Id, supplier.Id);
        afterVisit.ShouldNotBeNull();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Invalid business code provided. Please try again or contact Support.");
        afterVisit.IsClaimed.ShouldBe(beforeSnapshot.IsClaimed);
        afterVisit.ClaimedAt.ShouldBe(beforeSnapshot.ClaimedAt);
        afterVisit.TastingExperiencesClaimed.ShouldBe(beforeSnapshot.TastingExperiencesClaimed);
    }

    [Fact]
    public async Task SupplierVisit_when_tasting_experiences_exceed_total_guests_returns_validation_error_and_preserves_visit_works() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var seededVisit = Fixture.SeedData.SupplierVisitBookingVisits[1];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == seededVisit.SupplierId);
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);
        int tastingExperiencesClaimed = booking.TotalGuests + 1;

        var beforeVisit = await FindVisitAsync(booking.Id, supplier.Id);
        beforeVisit.ShouldNotBeNull();
        var beforeSnapshot = new VisitSnapshot(beforeVisit.IsClaimed, beforeVisit.ClaimedAt, beforeVisit.TastingExperiencesClaimed);

        using var request = CreateSupplierVisitPostRequest(
            booking.QrToken,
            supplier.Id,
            supplier.ConfirmationCode,
            tastingExperiencesClaimed,
            antiForgeryData);

        var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);
        string html = await ReadHtmlAsync(response, TestContext.Current.CancellationToken);
        var afterVisit = await FindVisitAsync(booking.Id, supplier.Id);
        afterVisit.ShouldNotBeNull();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain($"Cannot claim {tastingExperiencesClaimed} tasting experiences. This booking has only {booking.TotalGuests} guest(s).");
        afterVisit.IsClaimed.ShouldBe(beforeSnapshot.IsClaimed);
        afterVisit.ClaimedAt.ShouldBe(beforeSnapshot.ClaimedAt);
        afterVisit.TastingExperiencesClaimed.ShouldBe(beforeSnapshot.TastingExperiencesClaimed);
    }

    [Fact]
    public async Task SupplierVisit_when_visit_is_already_claimed_returns_validation_error_and_does_not_overwrite_values_works() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var seededVisit = Fixture.SeedData.SupplierVisitBookingVisits[2];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == seededVisit.SupplierId);
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);
        var forcedClaimedAt = DateTime.UtcNow.AddMinutes(-30);
        const int FORCED_TASTING_EXPERIENCES_CLAIMED = 2;

        var originalVisit = await FindVisitAsync(booking.Id, supplier.Id);
        originalVisit.ShouldNotBeNull();
        var originalSnapshot = new VisitSnapshot(originalVisit.IsClaimed, originalVisit.ClaimedAt, originalVisit.TastingExperiencesClaimed);

        try {
            await using (var context = Fixture.CreateDbContext()) {
                var visitToClaim = await context.Visits.SingleAsync(
                    visit => visit.BookingId == booking.Id && visit.SupplierId == supplier.Id,
                    TestContext.Current.CancellationToken);
                visitToClaim.IsClaimed = true;
                visitToClaim.ClaimedAt = forcedClaimedAt;
                visitToClaim.TastingExperiencesClaimed = FORCED_TASTING_EXPERIENCES_CLAIMED;
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            using var request = CreateSupplierVisitPostRequest(
                booking.QrToken,
                supplier.Id,
                supplier.ConfirmationCode,
                1,
                antiForgeryData);

            var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);
            string html = await ReadHtmlAsync(response, TestContext.Current.CancellationToken);
            var afterVisit = await FindVisitAsync(booking.Id, supplier.Id);
            afterVisit.ShouldNotBeNull();

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            html.ShouldContain("Please select a valid business to confirm the visit.");
            afterVisit.IsClaimed.ShouldBeTrue();
            afterVisit.ClaimedAt.ShouldBe(forcedClaimedAt);
            afterVisit.TastingExperiencesClaimed.ShouldBe(FORCED_TASTING_EXPERIENCES_CLAIMED);
        } finally {
            await using var context = Fixture.CreateDbContext();
            var visitToRestore = await context.Visits.SingleAsync(
                visit => visit.BookingId == booking.Id && visit.SupplierId == supplier.Id,
                TestContext.Current.CancellationToken);
            visitToRestore.IsClaimed = originalSnapshot.IsClaimed;
            visitToRestore.ClaimedAt = originalSnapshot.ClaimedAt;
            visitToRestore.TastingExperiencesClaimed = originalSnapshot.TastingExperiencesClaimed;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupplierVisit_when_qr_token_is_not_found_or_invalid_redirects_to_error_and_preserves_claimed_count_works(bool useEmptyToken) {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var seededVisit = Fixture.SeedData.SupplierVisitBookingVisits[0];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == seededVisit.SupplierId);
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);
        var qrToken = useEmptyToken ? Guid.Empty : Guid.NewGuid();
        int claimedCountBefore = await CountClaimedVisitsAsync(booking.Id);

        using var request = CreateSupplierVisitPostRequest(
            qrToken,
            supplier.Id,
            supplier.ConfirmationCode,
            1,
            antiForgeryData);

        var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);
        int claimedCountAfter = await CountClaimedVisitsAsync(booking.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldContain("/Home/Error");
        claimedCountAfter.ShouldBe(claimedCountBefore);
    }

    [Fact]
    public async Task SupplierVisit_when_booking_is_cancelled_redirects_to_tour_cancelled_and_preserves_claimed_count_works() {
        using var sut = CreateApiClient();
        var validBooking = Fixture.SeedData.SupplierVisitBooking;
        var cancelledBooking = Fixture.SeedData.CancelledBooking;
        var seededVisit = Fixture.SeedData.SupplierVisitBookingVisits[0];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == seededVisit.SupplierId);
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(validBooking.QrToken),
            TestContext.Current.CancellationToken);
        int claimedCountBefore = await CountClaimedVisitsAsync(validBooking.Id);

        using var request = CreateSupplierVisitPostRequest(
            cancelledBooking.QrToken,
            supplier.Id,
            supplier.ConfirmationCode,
            1,
            antiForgeryData);

        var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);
        int claimedCountAfter = await CountClaimedVisitsAsync(validBooking.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldContain("/Home/TourCancelled");
        claimedCountAfter.ShouldBe(claimedCountBefore);
    }

    [Fact]
    public async Task SupplierVisit_when_booking_supplier_has_no_visit_returns_validation_error_and_does_not_create_visit_works() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);
        int temporaryBookingSupplierId = 0;
        int selectedSupplierId;
        string supplierCode;

        await using (var context = Fixture.CreateDbContext()) {
            var existingSupplierIds = await context.BookingSuppliers
                .Where(bookingSupplier => bookingSupplier.BookingId == booking.Id)
                .Select(bookingSupplier => bookingSupplier.SupplierId)
                .ToListAsync(TestContext.Current.CancellationToken);

            var temporarySupplier = await context.Suppliers
                .Where(supplier => supplier.IsActive && !existingSupplierIds.Contains(supplier.Id))
                .OrderBy(supplier => supplier.Id)
                .FirstAsync(TestContext.Current.CancellationToken);
            selectedSupplierId = temporarySupplier.Id;
            supplierCode = temporarySupplier.ConfirmationCode;

            var temporaryBookingSupplier = new BookingSupplier {
                BookingId = booking.Id,
                SupplierId = selectedSupplierId
            };

            context.BookingSuppliers.Add(temporaryBookingSupplier);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            temporaryBookingSupplierId = temporaryBookingSupplier.Id;
        }

        try {
            using var request = CreateSupplierVisitPostRequest(
                booking.QrToken,
                selectedSupplierId,
                supplierCode,
                1,
                antiForgeryData);

            var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);
            string html = await ReadHtmlAsync(response, TestContext.Current.CancellationToken);
            var createdVisit = await FindVisitAsync(booking.Id, selectedSupplierId);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            html.ShouldContain("Visit not found for this booking and business. Please contact Support.");
            createdVisit.ShouldBeNull();
        } finally {
            await using var context = Fixture.CreateDbContext();
            var temporaryBookingSupplier = await context.BookingSuppliers.SingleOrDefaultAsync(
                bookingSupplier => bookingSupplier.Id == temporaryBookingSupplierId,
                TestContext.Current.CancellationToken);
            if (temporaryBookingSupplier != null) {
                context.BookingSuppliers.Remove(temporaryBookingSupplier);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }
    }

    [Fact]
    public async Task SupplierVisit_when_antiforgery_token_is_missing_rejects_request_and_preserves_visit_works() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var seededVisit = Fixture.SeedData.SupplierVisitBookingVisits[0];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == seededVisit.SupplierId);
        var beforeVisit = await FindVisitAsync(booking.Id, supplier.Id);
        beforeVisit.ShouldNotBeNull();
        var beforeSnapshot = new VisitSnapshot(beforeVisit.IsClaimed, beforeVisit.ClaimedAt, beforeVisit.TastingExperiencesClaimed);

        var formData = new Dictionary<string, string> {
            ["qrToken"] = booking.QrToken.ToString(),
            ["selectedSupplierId"] = supplier.Id.ToString(),
            ["supplierCode"] = supplier.ConfirmationCode,
            ["tastingExperiencesClaimed"] = "1"
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Home/SupplierVisit") {
            Content = new FormUrlEncodedContent(formData)
        };

        var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);
        var afterVisit = await FindVisitAsync(booking.Id, supplier.Id);
        afterVisit.ShouldNotBeNull();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        afterVisit.IsClaimed.ShouldBe(beforeSnapshot.IsClaimed);
        afterVisit.ClaimedAt.ShouldBe(beforeSnapshot.ClaimedAt);
        afterVisit.TastingExperiencesClaimed.ShouldBe(beforeSnapshot.TastingExperiencesClaimed);
    }

    [Fact]
    public async Task SupplierVisit_when_running_guard_checks_for_legacy_visit_recording_surfaces_no_forbidden_reintroductions_works() {
        using var sut = CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/visits/record") {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);
        using var scope = Fixture.Services.CreateScope();
        var endpointDataSource = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
        bool hasLegacyVisitRecordEndpoint = endpointDataSource.Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Any(endpoint => string.Equals(endpoint.RoutePattern.RawText, "api/visits/record", StringComparison.OrdinalIgnoreCase));

        bool hasVisitsController = typeof(global::FoodTour.Program).Assembly
            .GetTypes()
            .Any(type => string.Equals(type.Name, "VisitsController", StringComparison.Ordinal));

        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        hasLegacyVisitRecordEndpoint.ShouldBeFalse();
        hasVisitsController.ShouldBeFalse();
    }

    private static HttpRequestMessage CreateSupplierVisitPostRequest(
        Guid qrToken,
        int selectedSupplierId,
        string? supplierCode,
        int tastingExperiencesClaimed,
        AntiForgeryRequestData antiForgeryData) {
        var formData = new Dictionary<string, string> {
            ["qrToken"] = qrToken.ToString(),
            ["selectedSupplierId"] = selectedSupplierId.ToString(),
            ["supplierCode"] = supplierCode ?? string.Empty,
            ["tastingExperiencesClaimed"] = tastingExperiencesClaimed.ToString(),
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken
        };

        HttpRequestMessage result = new(HttpMethod.Post, "/Home/SupplierVisit") {
            Content = new FormUrlEncodedContent(formData)
        };
        result.Headers.Add("Cookie", antiForgeryData.CookieHeader);
        return result;
    }

    private static async Task<string> ReadHtmlAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
        string result = await response.Content.ReadAsStringAsync(cancellationToken);
        return result;
    }

    private async Task<Visit?> FindVisitAsync(int bookingId, int supplierId) {
        await using var context = Fixture.CreateDbContext();
        var result = await context.Visits.SingleOrDefaultAsync(
            visit => visit.BookingId == bookingId && visit.SupplierId == supplierId,
            TestContext.Current.CancellationToken);
        return result;
    }

    private async Task<int> CountClaimedVisitsAsync(int bookingId) {
        await using var context = Fixture.CreateDbContext();
        int result = await context.Visits.CountAsync(
            visit => visit.BookingId == bookingId && visit.IsClaimed,
            TestContext.Current.CancellationToken);
        return result;
    }

    private sealed record VisitSnapshot(
        bool IsClaimed,
        DateTime? ClaimedAt,
        int TastingExperiencesClaimed);
}
