namespace FoodTour.ITest.API.Tests;

public abstract class ApiTestBase(ApiApplicationFixture fixture) {
    protected ApiApplicationFixture Fixture => fixture;

    protected HttpClient CreateApiClient() {
        HttpClient result = Fixture.CreateApiClient();
        return result;
    }

    protected async Task CreateBookingAsync(Booking booking) {
        await using FoodTourContext context = Fixture.CreateDbContext();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    protected async Task<bool> BookingExistsAsync(string purchaseCode) {
        await using FoodTourContext context = Fixture.CreateDbContext();
        bool result = await context.Bookings.AnyAsync(booking => booking.PurchaseCode == purchaseCode, TestContext.Current.CancellationToken);
        return result;
    }

    protected async Task<Booking> GetBookingByPurchaseCodeAsync(string purchaseCode) {
        await using FoodTourContext context = Fixture.CreateDbContext();
        Booking? result = await context.Bookings.SingleOrDefaultAsync(booking => booking.PurchaseCode == purchaseCode, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        return result;
    }

    protected async Task<HttpResponseMessage> PostCheckBookingAsync(HttpClient client, string customerEmail, string purchaseCode) {
        BookingCheckRequest request = FoodTourApiRequestFactory.CreateCheckBookingRequest(customerEmail, purchaseCode);
        HttpResponseMessage result = await client.PostAsJsonAsync("/api/foodtour/checkbooking", request, TestContext.Current.CancellationToken);
        return result;
    }

    private protected async Task<HttpResponseMessage> PostNewBookingAsync(HttpClient client, BookingWebhookRequest request, string? key = null) {
        StringContent content = FoodTourApiRequestFactory.CreateNewBookingContent(request);
        string resolvedKey = key ?? FoodTourApiRequestFactory.ValidKey;
        HttpResponseMessage result = await client.PostAsync($"/api/foodtour/fhwebhook/newbooking?key={resolvedKey}", content, TestContext.Current.CancellationToken);
        return result;
    }

    private protected async Task<HttpResponseMessage> PostUpdateBookingAsync(HttpClient client, BookingWebhookRequest request, string? key = null) {
        StringContent content = FoodTourApiRequestFactory.CreateUpdateBookingContent(request);
        string resolvedKey = key ?? FoodTourApiRequestFactory.ValidKey;
        HttpResponseMessage result = await client.PostAsync($"/api/foodtour/fhwebhook/updatebooking?key={resolvedKey}", content, TestContext.Current.CancellationToken);
        return result;
    }

    private protected async Task<HttpResponseMessage> PostCancelBookingAsync(HttpClient client, CancelBookingWebhookRequest request, string? key = null) {
        StringContent content = FoodTourApiRequestFactory.CreateCancelBookingContent(request);
        string resolvedKey = key ?? FoodTourApiRequestFactory.ValidKey;
        HttpResponseMessage result = await client.PostAsync($"/api/foodtour/fhwebhook/cancelbooking?key={resolvedKey}", content, TestContext.Current.CancellationToken);
        return result;
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonElement result = JsonSerializer.Deserialize<JsonElement>(content);
        return result;
    }
}
