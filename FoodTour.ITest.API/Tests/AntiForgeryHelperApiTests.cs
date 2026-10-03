namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class AntiForgeryHelperApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task GetRequestDataAsync_ReturnsTokenAndCookies_WhenSupplierVisitPageLoads() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;

        var result = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);

        result.RequestToken.ShouldNotBeNullOrWhiteSpace();
        result.CookieHeader.ShouldContain("=");
    }

    [Fact]
    public async Task PostSupplierVisit_ReturnsRedirect_WhenAntiForgeryDataIsIncluded() {
        using var sut = CreateApiClient();
        TemporarySupplierVisitContext? temporaryContext = null;

        try {
            temporaryContext = await CreateTemporarySupplierVisitContextAsync();
            var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(
                sut,
                CreateSupplierVisitPath(temporaryContext.Booking.QrToken),
                TestContext.Current.CancellationToken);
            var formData = new Dictionary<string, string> {
                ["qrToken"] = temporaryContext.Booking.QrToken.ToString(),
                ["selectedSupplierId"] = temporaryContext.Supplier.Id.ToString(),
                ["supplierCode"] = temporaryContext.Supplier.ConfirmationCode,
                ["tastingExperiencesClaimed"] = temporaryContext.Booking.TotalGuests.ToString(),
                ["__RequestVerificationToken"] = antiForgeryData.RequestToken
            };
            HttpRequestMessage request = new(HttpMethod.Post, "/Home/SupplierVisit") {
                Content = new FormUrlEncodedContent(formData)
            };
            request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

            var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

            var result = await GetVisitAsync(temporaryContext.Booking.Id, temporaryContext.Supplier.Id);
            result.IsClaimed.ShouldBeTrue();
        } finally {
            if (temporaryContext != null) {
                await DeleteTemporarySupplierVisitContextAsync(temporaryContext.Booking.Id);
            }
        }
    }

    private async Task<TemporarySupplierVisitContext> CreateTemporarySupplierVisitContextAsync() {
        await using var context = Fixture.CreateDbContext();
        var supplier = await context.Suppliers
            .Where(currentSupplier => currentSupplier.IsActive)
            .OrderBy(currentSupplier => currentSupplier.Id)
            .Select(currentSupplier => new SeededSupplier(
                currentSupplier.Id,
                currentSupplier.Name,
                currentSupplier.ConfirmationCode,
                currentSupplier.ContactEmail,
                currentSupplier.IsActive))
            .FirstAsync(TestContext.Current.CancellationToken);

        Booking booking = new() {
            PurchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-antiforgery"),
            CustomerEmail = "api.antiforgery@example.com",
            CustomerName = "API Anti Forgery",
            TourDate = DateTime.SpecifyKind(DateTime.Today.AddHours(12), DateTimeKind.Utc),
            TotalGuests = 2,
            IsConfirmed = true,
            IsCancelled = false,
            RequiresEmailConfirmation = false,
            SelectedSupplierIds = [supplier.Id],
            BookingSuppliers = []
        };

        var bookingSupplier = new BookingSupplier { SupplierId = supplier.Id };
        booking.BookingSuppliers.Add(bookingSupplier);

        var visit = new Visit {
            Booking = booking,
            SupplierId = supplier.Id,
            IsClaimed = false,
            ClaimedAt = null,
            TastingExperiencesClaimed = 0
        };

        context.Bookings.Add(booking);
        context.Visits.Add(visit);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        TemporarySupplierVisitContext result = new(booking, supplier, visit);
        return result;
    }

    private async Task DeleteTemporarySupplierVisitContextAsync(int bookingId) {
        await using var context = Fixture.CreateDbContext();
        var booking = await context.Bookings
            .SingleOrDefaultAsync(currentBooking => currentBooking.Id == bookingId, CancellationToken.None);
        if (booking == null) {
            return;
        }

        var visits = await context.Visits
            .Where(visit => visit.BookingId == bookingId)
            .ToListAsync(CancellationToken.None);
        context.Visits.RemoveRange(visits);

        var bookingSuppliers = await context.BookingSuppliers
            .Where(bookingSupplier => bookingSupplier.BookingId == bookingId)
            .ToListAsync(CancellationToken.None);
        context.BookingSuppliers.RemoveRange(bookingSuppliers);

        context.Bookings.Remove(booking);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private sealed record TemporarySupplierVisitContext(Booking Booking, SeededSupplier Supplier, Visit Visit);
}
