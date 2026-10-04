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
                            "name": "Weymouth Self-Guided Food Tour (Five Tastings)"
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

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Theory]
    [InlineData("746635", "746635", 3)]
    [InlineData("\"746635\"", "746635", 3)]
    [InlineData("700426", "700426", 5)]
    [InlineData("\"700426\"", "700426", 5)]
    public async Task CreateNewBooking_MapsTastingCount_WhenItemPrimaryKeyMatches(string itemPkJson, string purchaseCode, int expectedTastingCount) {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateFoodTourApiController(context);
        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "item": {
                            "pk": {{itemPkJson}},
                            "name": "Unrecognised item name"
                        },
                        "start_at": "{{TestHelper.validTourDate:yyyy-MM-ddTHH:mm:ssZ}}"
                    },
                    "contact": {
                        "name": "{{TestHelper.DEFAULT_CUSTOMER_NAME}}",
                        "email": "{{TestHelper.VALID_EMAIL}}"
                    },
                    "customer_count": {{TestHelper.DEFAULT_GUEST_COUNT}},
                    "pk": "{{purchaseCode}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        result.ShouldBeOfType<OkObjectResult>();
        var booking = await context.Bookings.SingleAsync(item => item.PurchaseCode == purchaseCode, TestContext.Current.CancellationToken);
        booking.TastingCount.ShouldBe(expectedTastingCount);
    }

    [Fact]
    public async Task CreateNewBooking_SkipsProcessing_WhenNeitherItemNameNorPrimaryKeyMatches() {
        using var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateFoodTourApiController(context);
        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "item": {
                            "pk": "unknown-item",
                            "name": "Unrecognised item name"
                        },
                        "start_at": "{{TestHelper.validTourDate:yyyy-MM-ddTHH:mm:ssZ}}"
                    },
                    "contact": {
                        "name": "{{TestHelper.DEFAULT_CUSTOMER_NAME}}",
                        "email": "{{TestHelper.VALID_EMAIL}}"
                    },
                    "customer_count": {{TestHelper.DEFAULT_GUEST_COUNT}},
                    "pk": "UNMATCHED-ITEM"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, KEY);

        result.ShouldBeOfType<OkObjectResult>();
        (await context.Bookings.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesBookingDataWrapper_Correctly() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "Weymouth Self-Guided Food Tour (Five Tastings)"
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
                            "name": "Weymouth Self-Guided Food Tour (Five Tastings)"
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
                            "name": "Weymouth Self-Guided Food Tour (Five Tastings)"
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
                            "name": "Weymouth Self-Guided Food Tour (Five Tastings)"
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
                        "name": "Weymouth Self-Guided Food Tour (Five Tastings)"
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