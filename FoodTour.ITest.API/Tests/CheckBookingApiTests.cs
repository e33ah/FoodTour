namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class CheckBookingApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task CheckBooking_ReturnsFoundBooking_WhenSeededBookingExists() {
        using HttpClient sut = CreateApiClient();
        SeededBooking booking = Fixture.SeedData.BookingSelectionBooking;

        HttpResponseMessage response = await PostCheckBookingAsync(sut, booking.CustomerEmail, booking.PurchaseCode);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonElement result = await ReadJsonAsync(response);
        result.GetProperty("found").GetBoolean().ShouldBeTrue();
        result.GetProperty("isCancelled").GetBoolean().ShouldBeFalse();
        result.GetProperty("isConfirmed").GetBoolean().ShouldBe(booking.IsConfirmed);
        result.GetProperty("bookingId").GetInt32().ShouldBe(booking.Id);
    }

    [Fact]
    public async Task CheckBooking_ReturnsPartnerConfirmation_WhenVoucherMatchesButEmailDoesNot() {
        using HttpClient sut = CreateApiClient();
        SeededBooking booking = Fixture.SeedData.PartnerConfirmationBooking;

        HttpResponseMessage response = await PostCheckBookingAsync(sut, "wrong.email@example.com", booking.VoucherNumber!);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonElement result = await ReadJsonAsync(response);
        result.GetProperty("found").GetBoolean().ShouldBeFalse();
        result.GetProperty("requiresPartnerConfirmation").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task CheckBooking_ReturnsBadRequest_WhenRequestIsMissingRequiredFields() {
        using HttpClient sut = CreateApiClient();

        HttpResponseMessage response = await PostCheckBookingAsync(sut, string.Empty, string.Empty);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        JsonElement result = await ReadJsonAsync(response);
        result.GetProperty("found").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task CheckBooking_ReturnsNotFoundMessage_WhenBookingDoesNotExist() {
        using HttpClient sut = CreateApiClient();

        HttpResponseMessage response = await PostCheckBookingAsync(sut, "missing.booking@example.com", "NOT-A-REAL-BOOKING");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonElement result = await ReadJsonAsync(response);
        result.GetProperty("found").GetBoolean().ShouldBeFalse();
        result.TryGetProperty("requiresPartnerConfirmation", out _).ShouldBeFalse();
    }
}
