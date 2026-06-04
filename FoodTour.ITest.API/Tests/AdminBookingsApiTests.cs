namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public sealed class AdminBookingsApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task Bookings_ReturnsOk_WhenAuthenticated() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/Bookings", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Bookings_ReturnsBookingsList_WithCorrectCount() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/Bookings", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Bookings");
        html.ShouldContain(Fixture.SeedData.Bookings[0].CustomerEmail);
    }

    [Fact]
    public async Task Bookings_RedirectsToLogin_WhenNotAuthenticated() {
        using var client = CreateApiClient();

        var response = await client.GetAsync("/Admin/Bookings", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldContain("/Admin/Login");
    }

    [Fact]
    public async Task BookingDetail_ReturnsOk_WhenBookingExists() {
        using var client = CreateAuthenticatedClient();
        int bookingId = Fixture.SeedData.ConfirmedBooking.Id;

        var response = await client.GetAsync($"/Admin/BookingDetail/{bookingId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BookingDetail_ReturnsCorrectBookingData_WhenBookingExists() {
        using var client = CreateAuthenticatedClient();
        int bookingId = Fixture.SeedData.ConfirmedBooking.Id;

        var response = await client.GetAsync($"/Admin/BookingDetail/{bookingId}", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain(Fixture.SeedData.ConfirmedBooking.CustomerEmail);
        html.ShouldContain(Fixture.SeedData.ConfirmedBooking.PurchaseCode);
    }

    [Fact]
    public async Task BookingDetail_ReturnsNotFound_WhenBookingDoesNotExist() {
        using var client = CreateAuthenticatedClient();
        int nonExistentBookingId = 99999;

        var response = await client.GetAsync($"/Admin/BookingDetail/{nonExistentBookingId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Bookings_SupportsFiltering_ByUpcoming() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/Bookings?filter=upcoming", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain(Fixture.SeedData.ConfirmedBooking.CustomerEmail);
    }

    [Fact]
    public async Task Bookings_SupportsFiltering_ByCancelled() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/Bookings?filter=cancelled", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain(Fixture.SeedData.CancelledBooking.CustomerEmail);
    }

    private HttpClient CreateAuthenticatedClient() {
        var client = CreateApiClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }
}
