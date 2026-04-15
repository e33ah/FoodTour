namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerUpdateBookingTests : TestBase {
    [Fact]
    public async Task UpdateBooking_UpdatesBooking_WhenValidDataReceived() {
        var context = TestHelper.CreateInMemoryContext();
        var existingBooking = new Booking {
            PurchaseCode = TestHelper.UPDATE_TEST_CODE,
            CustomerEmail = "original@test.com",
            CustomerName = "Original Name",
            TourDate = DateTime.UtcNow.AddDays(30),
            TotalGuests = TestHelper.DEFAULT_GUEST_COUNT
        };
        context.Bookings.Add(existingBooking);
        await context.SaveChangesAsync(CancellationToken);

        var controller = TestHelper.CreateFoodTourApiController(context);

        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "start_at": "{{DateTime.UtcNow.AddDays(45):yyyy-MM-ddTHH:mm:ssZ}}",
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "contact": {
                        "email": "{{TestHelper.UPDATED_EMAIL}}",
                        "name": "{{TestHelper.UPDATED_CUSTOMER_NAME}}"
                    },
                    "customer_count": 4,
                    "pk": "{{TestHelper.UPDATE_TEST_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.UpdateBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);

        var updatedBooking = await context.Bookings.FirstOrDefaultAsync(b => b.PurchaseCode == TestHelper.UPDATE_TEST_CODE, CancellationToken);
        updatedBooking.ShouldNotBeNull();
        updatedBooking.CustomerEmail.ShouldBe(TestHelper.UPDATED_EMAIL);
        updatedBooking.CustomerName.ShouldBe(TestHelper.UPDATED_CUSTOMER_NAME);
        updatedBooking.TotalGuests.ShouldBe(4);
    }

    [Fact]
    public async Task UpdateBooking_ReturnsUnauthorised_WhenInvalidKey() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "pk": "TEST_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.UpdateBooking(jsonElement, TestHelper.INVALID_KEY);

        var unauthorisedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorisedResult.StatusCode.ShouldBe(401);
        unauthorisedResult.Value.ShouldBe("Invalid key provided.");
    }

    [Fact]
    public async Task UpdateBooking_ReturnsUnauthorised_WhenMissingKey() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "pk": "TEST_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.UpdateBooking(jsonElement, null!);

        var unauthorisedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorisedResult.StatusCode.ShouldBe(401);
    }

    [Fact]
    public async Task UpdateBooking_ReturnsOk_WhenBookingNotFound() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "start_at": "2025-06-15T10:00:00Z",
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "contact": {
                        "email": "test@example.com",
                        "name": "Test User"
                    },
                    "customer_count": {{TestHelper.DEFAULT_GUEST_COUNT}},
                    "pk": "NONEXISTENT_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.UpdateBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task UpdateBooking_SkipsUpdate_WhenNoChangesDetected() {
        var context = TestHelper.CreateInMemoryContext();
        var tourDate = DateTime.UtcNow.AddDays(30);
        var existingBooking = new Booking {
            PurchaseCode = TestHelper.NO_CHANGE_TEST_CODE,
            CustomerEmail = "same@test.com",
            CustomerName = "Same Name",
            TourDate = tourDate,
            TotalGuests = 3
        };
        context.Bookings.Add(existingBooking);
        await context.SaveChangesAsync(CancellationToken);

        var controller = TestHelper.CreateFoodTourApiController(context);

        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "start_at": "{{tourDate:yyyy-MM-ddTHH:mm:ssZ}}",
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "contact": {
                        "email": "same@test.com",
                        "name": "Same Name"
                    },
                    "customer_count": 3,
                    "pk": "{{TestHelper.NO_CHANGE_TEST_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.UpdateBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task UpdateBooking_HandlesInvalidJson_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();
        var jsonElement = new JsonElement();

        var result = await controller.UpdateBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task UpdateBooking_HandlesValidationFailure_Gracefully() {
        var context = TestHelper.CreateInMemoryContext();
        var existingBooking = new Booking {
            PurchaseCode = TestHelper.VALIDATION_FAIL_TEST_CODE,
            CustomerEmail = "test@test.com",
            CustomerName = "Test User",
            TourDate = DateTime.UtcNow.AddDays(30),
            TotalGuests = TestHelper.DEFAULT_GUEST_COUNT
        };
        context.Bookings.Add(existingBooking);
        await context.SaveChangesAsync(CancellationToken);

        var controller = TestHelper.CreateFoodTourApiController(context);

        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "start_at": "{{TestHelper.pastTourDate:yyyy-MM-ddTHH:mm:ssZ}}",
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "contact": {
                        "email": "{{TestHelper.INVALID_EMAIL}}",
                        "name": "Test User"
                    },
                    "customer_count": {{TestHelper.INVALID_GUEST_COUNT}},
                    "pk": "{{TestHelper.VALIDATION_FAIL_TEST_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.UpdateBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }
    [Fact]
    public async Task CreateNewBooking_ReturnsUnauthorised_WhenMissingKey() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "pk": "TEST_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, null!);

        var unauthorisedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorisedResult.StatusCode.ShouldBe(401);
    }

    [Fact]
    public async Task UpdateBooking_SkipsProcessing_WhenNotWeymouthTour() {
        var controller = TestHelper.CreateFoodTourApiController();
        const string DIFFERENT_TOUR_NAME = "Different Tour";
        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "{{DIFFERENT_TOUR_NAME}}"
                        }
                    },
                    "pk": "OTHER_TOUR_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.UpdateBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }
}
