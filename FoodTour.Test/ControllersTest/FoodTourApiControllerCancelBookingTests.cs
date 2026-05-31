namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerCancelBookingTests : TestBase {
    [Fact]
    public async Task CancelBooking_MarksBookingAsCancelled_WhenValid() {
        var context = TestHelper.CreateInMemoryContext();
        var existingBooking = new Booking {
            PurchaseCode = TestHelper.CANCEL_TEST_CODE,
            CustomerEmail = "cancel@test.com",
            CustomerName = "Cancel Test",
            TourDate = DateTime.UtcNow.AddDays(30),
            TotalGuests = TestHelper.DEFAULT_GUEST_COUNT,
            IsCancelled = false
        };
        context.Bookings.Add(existingBooking);
        await context.SaveChangesAsync(CancellationToken);

        var controller = TestHelper.CreateFoodTourApiController(context);

        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "pk": "{{TestHelper.CANCEL_TEST_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CancelBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);

        var cancelledBooking = await context.Bookings.FirstOrDefaultAsync(b => b.PurchaseCode == TestHelper.CANCEL_TEST_CODE, CancellationToken);
        cancelledBooking.ShouldNotBeNull();
        cancelledBooking.IsCancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task CancelBooking_ReturnsUnauthorised_WhenInvalidKey() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "pk": "TEST_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CancelBooking(jsonElement, TestHelper.INVALID_KEY);

        var unauthorisedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorisedResult.StatusCode.ShouldBe(401);
        unauthorisedResult.Value.ShouldBe("Invalid key provided.");
    }

    [Fact]
    public async Task CancelBooking_ReturnsUnauthorised_WhenMissingKey() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = """
            {
                "booking": {
                    "pk": "TEST_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CancelBooking(jsonElement, null!);

        var unauthorisedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorisedResult.StatusCode.ShouldBe(401);
    }

    [Fact]
    public async Task CancelBooking_ReturnsOk_WhenBookingNotFound() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "pk": "NONEXISTENT_CANCEL_CODE"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CancelBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CancelBooking_HandlesInvalidJson_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();
        var jsonElement = new JsonElement();

        var result = await controller.CancelBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CancelBooking_HandlesValidationFailure_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "pk": ""
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CancelBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CancelBooking_HandlesDatabaseFailure_Gracefully() {
        var context = TestHelper.CreateContextFailingOnSave();
        var existingBooking = new Booking {
            PurchaseCode = TestHelper.DB_FAIL_TEST_CODE,
            CustomerEmail = "dbfail@test.com",
            CustomerName = "DB Fail Test",
            TourDate = DateTime.UtcNow.AddDays(30),
            TotalGuests = TestHelper.DEFAULT_GUEST_COUNT,
            IsCancelled = false
        };
        context.Bookings.Add(existingBooking);

        var controller = TestHelper.CreateFoodTourApiController(context);

        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "pk": "{{TestHelper.DB_FAIL_TEST_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CancelBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CancelBooking_SkipsProcessing_WhenNotWeymouthTour() {
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

        var result = await controller.CancelBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }
}
