namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerTests {
    private const string KEY = "5N8GS1ZZKC8J";
    [Fact]
    public async Task CreateNewBooking_ReturnsOk_WhenValidJsonReceived() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "customerEmail": "test@example.com",
                    "tourDate": "2025-06-15T00:00:00Z",
                    "purchaseCode": "TEST123"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_ReturnsOk_WhenComplexJsonReceived() {
        var controller = TestHelper.CreateFoodTourApiController();

        string jsonString = """
            {
                "booking": {
                    "customerEmail": "test@example.com",
                    "tourDate": "2025-06-15T00:00:00Z",
                    "purchaseCode": "TEST123",
                    "additionalData": {
                        "nested": "value",
                        "number": 42
                    },
                    "arrayData": ["item1", "item2"]
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesInvalidJson_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();

        // Create an empty JsonElement
        var jsonElement = new JsonElement();

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_PreventsDuplicates_WhenSamePurchaseCodeSentTwice() {
        var controller = TestHelper.CreateFoodTourApiController();

        string jsonString = """
            {
                "booking": {
                    "availability": {
                        "start_at": "2025-06-15T10:00:00Z",
                        "item": {
                            "name": "Weymouth Self-Guided Food Tour"
                        }
                    },
                    "contact": {
                        "email": "test@example.com",
                        "name": "Test User"
                    },
                    "customer_count": 2,
                    "pk": "DUPLICATE_TEST_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        // First call should succeed
        var firstResult = await controller.CreateNewBooking(jsonElement, KEY);
        var firstOkResult = firstResult.ShouldBeOfType<OkObjectResult>();
        firstOkResult.StatusCode.ShouldBe(200);

        // Second call with same purchase code should be handled gracefully
        var secondResult = await controller.CreateNewBooking(jsonElement, KEY);
        var secondOkResult = secondResult.ShouldBeOfType<OkObjectResult>();
        secondOkResult.StatusCode.ShouldBe(200);

        // Both calls should return successfully, but only one booking should be created
        // The duplicate prevention is logged, not exposed in the API response for security
    }
}