namespace FoodTour.ITest.API.Tests;

public abstract class ApiTestBase(ApiApplicationFixture fixture) {
    protected ApiApplicationFixture Fixture => fixture;

    protected HttpClient CreateApiClient() {
        var result = Fixture.CreateApiClient();
        return result;
    }

    protected async Task CreateBookingAsync(Booking booking) {
        await using var context = Fixture.CreateDbContext();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    protected async Task<bool> BookingExistsAsync(string purchaseCode) {
        await using var context = Fixture.CreateDbContext();
        bool result = await context.Bookings.AnyAsync(booking => booking.PurchaseCode == purchaseCode, TestContext.Current.CancellationToken);
        return result;
    }

    protected async Task<Booking> GetBookingByPurchaseCodeAsync(string purchaseCode) {
        await using var context = Fixture.CreateDbContext();
        var result = await context.Bookings.SingleOrDefaultAsync(booking => booking.PurchaseCode == purchaseCode, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        return result;
    }

    protected async Task<Visit> GetVisitAsync(int bookingId, int supplierId) {
        await using var context = Fixture.CreateDbContext();
        var result = await context.Visits.SingleOrDefaultAsync(
            visit => visit.BookingId == bookingId && visit.SupplierId == supplierId,
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        return result;
    }

    protected async Task<HttpResponseMessage> PostCheckBookingAsync(HttpClient client, string customerEmail, string purchaseCode) {
        var request = FoodTourApiRequestFactory.CreateCheckBookingRequest(customerEmail, purchaseCode);
        var result = await client.PostAsJsonAsync("/api/foodtour/checkbooking", request, TestContext.Current.CancellationToken);
        return result;
    }

    private protected async Task<HttpResponseMessage> PostNewBookingAsync(HttpClient client, BookingWebhookRequest request, string? key = null) {
        var content = FoodTourApiRequestFactory.CreateNewBookingContent(request);
        string resolvedKey = key ?? FoodTourApiRequestFactory.ValidKey;
        var result = await client.PostAsync($"/api/foodtour/fhwebhook/newbooking?key={resolvedKey}", content, TestContext.Current.CancellationToken);
        return result;
    }

    private protected async Task<HttpResponseMessage> PostUpdateBookingAsync(HttpClient client, BookingWebhookRequest request, string? key = null) {
        var content = FoodTourApiRequestFactory.CreateUpdateBookingContent(request);
        string resolvedKey = key ?? FoodTourApiRequestFactory.ValidKey;
        var result = await client.PostAsync($"/api/foodtour/fhwebhook/updatebooking?key={resolvedKey}", content, TestContext.Current.CancellationToken);
        return result;
    }

    private protected async Task<HttpResponseMessage> PostCancelBookingAsync(HttpClient client, CancelBookingWebhookRequest request, string? key = null) {
        var content = FoodTourApiRequestFactory.CreateCancelBookingContent(request);
        string resolvedKey = key ?? FoodTourApiRequestFactory.ValidKey;
        var result = await client.PostAsync($"/api/foodtour/fhwebhook/cancelbooking?key={resolvedKey}", content, TestContext.Current.CancellationToken);
        return result;
    }

    private protected async Task<HttpResponseMessage> PutRecordVisitAsync(HttpClient client, RecordVisitRequest request) {
        var antiForgery = await AntiForgeryTokenHelper.GetRequestDataAsync(
            client,
            VisitsApiRequestFactory.CreateSupplierVisitPath(request.QrToken),
            TestContext.Current.CancellationToken);
        HttpRequestMessage message = new(HttpMethod.Put, "/api/visits/record") {
            Content = VisitsApiRequestFactory.CreateRecordVisitContent(request)
        };
        message.Headers.Add("RequestVerificationToken", antiForgery.RequestToken);
        message.Headers.Add("Cookie", antiForgery.CookieHeader);

        var result = await client.SendAsync(message, TestContext.Current.CancellationToken);
        return result;
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result;
    }
}
