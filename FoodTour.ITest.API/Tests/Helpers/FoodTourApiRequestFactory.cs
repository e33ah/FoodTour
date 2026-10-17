namespace FoodTour.ITest.API.Tests.Helpers;

internal static class FoodTourApiRequestFactory {
    private const string VALID_KEY = "api-integration-test-webhook-key";
    private const string WEYMOUTH_TOUR_NAME = "Weymouth Self-Guided Food Tour (Five Tastings)";

    public static string ValidKey => VALID_KEY;

    public static BookingCheckRequest CreateCheckBookingRequest(string customerEmail, string purchaseCode) {
        BookingCheckRequest result = new() {
            CustomerEmail = customerEmail,
            PurchaseCode = purchaseCode
        };

        return result;
    }

    public static StringContent CreateNewBookingContent(BookingWebhookRequest request) {
        string json = $$"""
            {
                "booking": {
                    "pk": "{{request.PurchaseCode}}",
                    "contact": {
                        "name": "{{request.CustomerName}}",
                        "email": "{{request.CustomerEmail}}"
                    },
                    "availability": {
                        "start_at": "{{request.TourDate:yyyy-MM-ddTHH:mm:ssZ}}",
                        "item": {
                            "name": "{{request.ItemName}}"
                        }
                    },
                    "customer_count": {{request.CustomerCount}},
                    "voucher_number": {{FormatJsonString(request.VoucherNumber)}}
                }
            }
            """;

        var result = CreateJsonContent(json);
        return result;
    }

    public static StringContent CreateUpdateBookingContent(BookingWebhookRequest request) {
        return CreateNewBookingContent(request);
    }

    public static StringContent CreateCancelBookingContent(CancelBookingWebhookRequest request) {
        string json = $$"""
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "{{request.ItemName}}"
                        }
                    },
                    "pk": "{{request.PurchaseCode}}"
                }
            }
            """;

        var result = CreateJsonContent(json);
        return result;
    }

    public static string CreateUniquePurchaseCode(string prefix) {
        string result = $"{prefix}-{Guid.NewGuid():N}"[..18];
        return result;
    }

    private static StringContent CreateJsonContent(string json) {
        StringContent result = new(json, Encoding.UTF8, "application/json");
        return result;
    }

    private static string FormatJsonString(string? value) {
        string result = value is null
            ? "null"
            : JsonSerializer.Serialize(value);
        return result;
    }
}

internal sealed class BookingWebhookRequest {
    public required string PurchaseCode { get; init; }

    public required string CustomerEmail { get; init; }

    public required string CustomerName { get; init; }

    public required DateTime TourDate { get; init; }

    public int CustomerCount { get; init; } = 2;

    public string? VoucherNumber { get; init; }

    public string ItemName { get; init; } = "Weymouth Self-Guided Food Tour (Five Tastings)";
}

internal sealed class CancelBookingWebhookRequest {
    public required string PurchaseCode { get; init; }

    public string ItemName { get; init; } = "Weymouth Self-Guided Food Tour (Five Tastings)";
}
