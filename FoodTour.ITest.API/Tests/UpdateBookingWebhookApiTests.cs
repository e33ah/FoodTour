namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class UpdateBookingWebhookApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task UpdateBooking_ReturnsOk_WhenBookingDoesNotExist() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-miss");
        BookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode,
            CustomerEmail = "missing.update@example.com",
            CustomerName = "Missing Update",
            TourDate = DateTime.UtcNow.AddDays(40),
            CustomerCount = 4
        };

        var response = await PostUpdateBookingAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        bool bookingExists = await BookingExistsAsync(purchaseCode);
        bookingExists.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateBooking_LeavesBookingUnchanged_WhenWebhookMatchesExistingValues() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-same");
        var tourDate = DateTime.UtcNow.AddDays(41);

        await CreateBookingAsync(new Booking {
            PurchaseCode = purchaseCode,
            CustomerEmail = "same.values@example.com",
            CustomerName = "Same Values",
            TourDate = tourDate,
            TotalGuests = 2,
            VoucherNumber = null,
            RequiresEmailConfirmation = false
        });

        BookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode,
            CustomerEmail = "same.values@example.com",
            CustomerName = "Same Values",
            TourDate = tourDate,
            CustomerCount = 2,
            VoucherNumber = null
        };

        var response = await PostUpdateBookingAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await GetBookingByPurchaseCodeAsync(purchaseCode);
        result.CustomerEmail.ShouldBe("same.values@example.com");
        result.CustomerName.ShouldBe("Same Values");
        result.TotalGuests.ShouldBe(2);
        result.VoucherNumber.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateBooking_PersistsChanges_WhenWebhookPayloadIsValid() {
        using var sut = CreateApiClient();
        string purchaseCode = FoodTourApiRequestFactory.CreateUniquePurchaseCode("api-upd");

        await CreateBookingAsync(new Booking {
            PurchaseCode = purchaseCode,
            CustomerEmail = "before.update@example.com",
            CustomerName = "Before Update",
            TourDate = DateTime.UtcNow.AddDays(42),
            TotalGuests = 2,
            VoucherNumber = "OLD-VOUCHER",
            RequiresEmailConfirmation = true
        });

        BookingWebhookRequest request = new() {
            PurchaseCode = purchaseCode,
            CustomerEmail = "after.update@example.com",
            CustomerName = "After Update",
            TourDate = DateTime.UtcNow.AddDays(45),
            CustomerCount = 5,
            VoucherNumber = "NEW-VOUCHER"
        };

        var response = await PostUpdateBookingAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await GetBookingByPurchaseCodeAsync(purchaseCode);
        result.CustomerEmail.ShouldBe("after.update@example.com");
        result.CustomerName.ShouldBe("After Update");
        result.TotalGuests.ShouldBe(5);
        result.VoucherNumber.ShouldBe("NEW-VOUCHER");
    }
}
