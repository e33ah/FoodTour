namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class CancelBookingWebhookApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task CancelBooking_ReturnsOk_WhenBookingDoesNotExist() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-none");
        CancelBookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode
        };

        var response = await PostCancelBookingAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelBooking_MarksBookingAsCancelled_WhenWebhookPayloadIsValid() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-can");

        await CreateBookingAsync(new Booking {
            PurchaseCode = purchaseCode,
            CustomerEmail = "cancel.me@example.com",
            CustomerName = "Cancel Me",
            TourDate = DateTime.UtcNow.AddDays(46),
            TotalGuests = 2,
            IsCancelled = false
        });

        CancelBookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode
        };

        var response = await PostCancelBookingAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await GetBookingByPurchaseCodeAsync(purchaseCode);
        result.IsCancelled.ShouldBeTrue();
    }
}
