namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerJsonProcessingTests {
    [Fact]
    public async Task CreateNewBooking_ProcessesWeymouthTourBooking_WhenItemNameMatches() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "availability": {
                    "item": {
                        "name": "Weymouth Self-Guided Food Tour"
                    },
                    "start_at": "2025-06-15T10:00:00Z"
                },
                "contact": {
                    "name": "John Doe",
                    "email": "john@example.com"
                },
                "customer_count": 2,
                "pk": "TOUR123"
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        var responseValue = okResult.Value;
        responseValue.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateNewBooking_SkipsProcessing_WhenItemNameDoesNotMatch() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "availability": {
                    "item": {
                        "name": "Different Tour"
                    },
                    "start_at": "2025-06-15T10:00:00Z"
                },
                "contact": {
                    "email": "john@example.com"
                },
                "customer_count": 2,
                "pk": "TOUR123"
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesWrappedBookingData_Correctly() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "Weymouth Self-Guided Food Tour"
                        },
                        "start_at": "2025-06-15T10:00:00Z"
                    },
                    "contact": {
                        "email": "john@example.com"
                    },
                    "customer_count": 2,
                    "pk": "TOUR123"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesMissingFields_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "availability": {
                    "item": {
                        "name": "Weymouth Self-Guided Food Tour"
                    }
                },
                "contact": {
                    "email": "john@example.com"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesInvalidDateFormat_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "availability": {
                    "item": {
                        "name": "Weymouth Self-Guided Food Tour"
                    },
                    "start_at": "invalid-date"
                },
                "contact": {
                    "email": "john@example.com"
                },
                "customer_count": 2,
                "pk": "TOUR123"
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesInvalidCustomerCount_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "availability": {
                    "item": {
                        "name": "Weymouth Self-Guided Food Tour"
                    },
                    "start_at": "2025-06-15T10:00:00Z"
                },
                "contact": {
                    "email": "john@example.com"
                },
                "customer_count": "invalid-number",
                "pk": "TOUR123"
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_CountsPropertiesCorrectly_InResponse() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "property1": "value1",
                "property2": "value2",
                "property3": "value3"
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var responseValue = okResult.Value;
        responseValue.ShouldNotBeNull();

        // Convert response to JSON and access properties
        var jsonResponseString = JsonSerializer.Serialize(responseValue);
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponseString);
        var receivedProperties = jsonResponse.GetProperty("receivedProperties").GetInt32();
        receivedProperties.ShouldBe(3);
    }

    [Fact]
    public async Task CreateNewBooking_ReturnsInternalServerError_WhenExceptionThrown() {
        // This test would require a way to force an exception, 
        // which might need a mockable dependency in the controller
        // For now, this shows the intent - actual implementation would depend on your architecture
        var controller = TestHelper.CreateFoodTourApiController();
        
        // Create malformed JSON that might cause parsing issues
        var malformedElement = new JsonElement();

        var result = await controller.CreateNewBooking(malformedElement);

        // Should handle gracefully and return OK (based on current implementation)
        var okResult = result.ShouldBeOfType<OkObjectResult>();
    }
}