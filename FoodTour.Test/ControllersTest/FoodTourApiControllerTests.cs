namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerTests {
    [Fact]
    public async Task CreateNewBooking_ReturnsOk_WhenValidJsonReceived() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new FoodTourContext(options);
        var controller = new FoodTourApiController(context);
        string jsonString = """
            {
                "customerEmail": "test@example.com",
                "tourDate": "2025-06-15T00:00:00Z",
                "purchaseCode": "TEST123"
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_ReturnsOk_WhenComplexJsonReceived() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new FoodTourContext(options);
        var controller = new FoodTourApiController(context);

        string jsonString = """
            {
                "customerEmail": "test@example.com",
                "tourDate": "2025-06-15T00:00:00Z",
                "purchaseCode": "TEST123",
                "additionalData": {
                    "nested": "value",
                    "number": 42
                },
                "arrayData": ["item1", "item2"]
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);

        var responseValue = okResult.Value as dynamic;
        // Just verify we get a successful response - the logging is the main goal
    }

    [Fact]
    public async Task CreateNewBooking_HandlesInvalidJson_Gracefully() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new FoodTourContext(options);
        var controller = new FoodTourApiController(context);

        // Create an empty JsonElement
        var jsonElement = new JsonElement();

        var result = await controller.CreateNewBooking(jsonElement);

        // Should still return OK as we're just logging at this stage
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }
}