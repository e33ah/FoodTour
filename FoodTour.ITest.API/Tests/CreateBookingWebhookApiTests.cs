namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class CreateBookingWebhookApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task CreateNewBooking_ReturnsUnauthorised_WhenKeyIsInvalid() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-new");
        BookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode,
            CustomerEmail = "invalid.key@example.com",
            CustomerName = "Invalid Key",
            TourDate = DateTime.UtcNow.AddDays(30)
        };

        var response = await PostNewBookingAsync(sut, request, "invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateNewBooking_DoesNotPersistBooking_WhenWebhookIsForDifferentTour() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-other");
        BookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode,
            CustomerEmail = "other.tour@example.com",
            CustomerName = "Other Tour",
            TourDate = DateTime.UtcNow.AddDays(31),
            ItemName = "Different Tour"
        };

        var response = await PostNewBookingAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        bool bookingExists = await BookingExistsAsync(purchaseCode);
        bookingExists.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateNewBooking_PersistsBooking_WhenWebhookPayloadIsValid() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-save");
        const string CUSTOMER_EMAIL = "api.create@example.com";
        BookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode,
            CustomerEmail = CUSTOMER_EMAIL,
            CustomerName = "API Create",
            TourDate = DateTime.UtcNow.AddDays(32),
            CustomerCount = 3,
            VoucherNumber = "API-VOUCHER-001"
        };

        var response = await PostNewBookingAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var booking = await GetBookingByPurchaseCodeAsync(purchaseCode);
        booking.CustomerEmail.ShouldBe(CUSTOMER_EMAIL);
        booking.TotalGuests.ShouldBe(3);
        booking.VoucherNumber.ShouldBe("API-VOUCHER-001");
    }

    [Fact]
    public async Task CreateNewBooking_SetsTastingCount_WhenThreeTastingItemPosted() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-three");
        const string CUSTOMER_EMAIL = "api.three@example.com";
        BookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode,
            CustomerEmail = CUSTOMER_EMAIL,
            CustomerName = "API Three",
            TourDate = DateTime.UtcNow.AddDays(33),
            CustomerCount = 2,
            VoucherNumber = "API-VOUCHER-THREE",
            ItemName = "Weymouth Self-Guided Food Tour (Three Tastings)"
        };

        var response = await PostNewBookingAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var booking = await GetBookingByPurchaseCodeAsync(purchaseCode);
        booking.CustomerEmail.ShouldBe(CUSTOMER_EMAIL);
        booking.TastingCount.ShouldBe(3);
    }
}
