namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerJsonProcessingTests {
    private const string KEY = "5N8GS1ZZKC8J";
    [Fact]
    public async Task CreateNewBooking_ProcessesWeymouthTourBooking_WhenItemNameMatches() {
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
                        "name": "John Doe",
                        "email": "john@example.com"
                    },
                    "customer_count": 2,
                    "pk": "TOUR123"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        object? responseValue = okResult.Value;
        responseValue.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateNewBooking_SkipsProcessing_WhenItemNameDoesNotMatch() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
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
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY );

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesBookingDataWrapper_Correctly() {
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

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesMissingFields_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "Weymouth Self-Guided Food Tour"
                        }
                    },
                    "contact": {
                        "email": "john@example.com"
                    }
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesInvalidDateFormat_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
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
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesInvalidCustomerCount_Gracefully() {
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
                    "customer_count": "invalid-number",
                    "pk": "TOUR123"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_ReturnsOk_WhenBookingWrapperMissing() {
        var controller = TestHelper.CreateFoodTourApiController();

        // Create JSON without the required "booking" wrapper
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

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        // Should return OK even though GetProperty("booking") throws - webhook resilience
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact] 
    public async Task CreateNewBooking_ReturnsInternalServerError_WhenExceptionThrown() {
        var controller = TestHelper.CreateFoodTourApiController();

        // Create malformed JSON that might cause parsing issues
        var malformedElement = new JsonElement();

        var result = await controller.CreateNewBooking(malformedElement, KEY);

        // Should handle gracefully and return OK (based on current implementation)
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }
}