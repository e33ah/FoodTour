namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerWebhookAuthenticationTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData(TestHelper.INVALID_KEY)]
    [InlineData("UNIT-TEST-WEBHOOK-KEY")]
    public async Task WebhookActions_ReturnUnauthorised_WhenKeyIsInvalid(string? key) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateFoodTourApiController(context);
        var jsonData = JsonSerializer.Deserialize<JsonElement>("{}");

        var results = new[] {
            await sut.CreateNewBooking(jsonData, key),
            await sut.UpdateBooking(jsonData, key),
            await sut.CancelBooking(jsonData, key)
        };

        foreach (var result in results) {
            result.ShouldBeOfType<UnauthorizedObjectResult>();
        }
        context.Bookings.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task WebhookActions_ReturnUnauthorised_WhenConfiguredKeyIsBlank(string? key) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateFoodTourApiController(context, key);
        var jsonData = JsonSerializer.Deserialize<JsonElement>("{}");

        var results = new[] {
            await sut.CreateNewBooking(jsonData, key),
            await sut.UpdateBooking(jsonData, key),
            await sut.CancelBooking(jsonData, key)
        };

        foreach (var result in results) {
            result.ShouldBeOfType<UnauthorizedObjectResult>();
        }
        context.Bookings.ShouldBeEmpty();
    }

    [Fact]
    public async Task WebhookActions_UseConfiguredKey_WhenKeyChanges() {
        const string CONFIGURED_KEY = "different-unit-test-webhook-key";
        using var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateFoodTourApiController(context, CONFIGURED_KEY);
        var jsonData = JsonSerializer.Deserialize<JsonElement>("{}");

        var acceptedResults = new[] {
            await sut.CreateNewBooking(jsonData, CONFIGURED_KEY),
            await sut.UpdateBooking(jsonData, CONFIGURED_KEY),
            await sut.CancelBooking(jsonData, CONFIGURED_KEY)
        };
        var rejectedResults = new[] {
            await sut.CreateNewBooking(jsonData, TestHelper.VALID_KEY),
            await sut.UpdateBooking(jsonData, TestHelper.VALID_KEY),
            await sut.CancelBooking(jsonData, TestHelper.VALID_KEY)
        };

        foreach (var result in acceptedResults) {
            result.ShouldBeOfType<OkObjectResult>();
        }
        foreach (var result in rejectedResults) {
            result.ShouldBeOfType<UnauthorizedObjectResult>();
        }
    }
}
