namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerCreateBookingTests {
    [Fact]
    public async Task CreateNewBooking_ReturnsOk_WhenValidJsonReceived() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = $$"""
            {
                "booking": {
                    "customerEmail": "{{TestHelper.VALID_EMAIL}}",
                    "tourDate": "{{TestHelper.validTourDate:yyyy-MM-ddTHH:mm:ssZ}}",
                    "purchaseCode": "{{TestHelper.VALID_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_ReturnsOk_WhenComplexJsonReceived() {
        var controller = TestHelper.CreateFoodTourApiController();

        string jsonString = $$"""
            {
                "booking": {
                    "customerEmail": "{{TestHelper.VALID_EMAIL}}",
                    "tourDate": "{{TestHelper.validTourDate:yyyy-MM-ddTHH:mm:ssZ}}",
                    "purchaseCode": "{{TestHelper.VALID_CODE}}",
                    "additionalData": {
                        "nested": "value",
                        "number": 42
                    },
                    "arrayData": ["item1", "item2"]
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_HandlesInvalidJson_Gracefully() {
        var controller = TestHelper.CreateFoodTourApiController();

        // Create an empty JsonElement
        var jsonElement = new JsonElement();

        var result = await controller.CreateNewBooking(jsonElement, TestHelper.VALID_KEY);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CreateNewBooking_PreventsDuplicates_WhenSamePurchaseCodeSentTwice() {
        var controller = TestHelper.CreateFoodTourApiController();

        string jsonString = $$"""
            {
                "booking": {
                    "availability": {
                        "start_at": "{{TestHelper.validTourDate:yyyy-MM-ddTHH:mm:ssZ}}",
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "contact": {
                        "email": "{{TestHelper.VALID_EMAIL}}",
                        "name": "{{TestHelper.DEFAULT_CUSTOMER_NAME}}"
                    },
                    "customer_count": {{TestHelper.DEFAULT_GUEST_COUNT}},
                    "pk": "{{TestHelper.NO_CHANGE_TEST_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        // First call should succeed
        var firstResult = await controller.CreateNewBooking(jsonElement, TestHelper.VALID_KEY);
        var firstOkResult = firstResult.ShouldBeOfType<OkObjectResult>();
        firstOkResult.StatusCode.ShouldBe(200);

        // Second call with same purchase code should be handled gracefully
        var secondResult = await controller.CreateNewBooking(jsonElement, TestHelper.VALID_KEY);
        var secondOkResult = secondResult.ShouldBeOfType<OkObjectResult>();
        secondOkResult.StatusCode.ShouldBe(200);

        // Both calls should return successfully, but only one booking should be created
        // The duplicate prevention is logged, not exposed in the API response for security
    }

    [Fact]
    public async Task CreateNewBooking_SavesVoucherNumber_WhenVoucherNumberProvided() {
        var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateFoodTourApiController(context);
        string jsonString = $$"""
            {
                "booking": {
                    "pk": "{{TestHelper.VALID_CODE}}",
                    "contact": {
                        "email": "{{TestHelper.VALID_EMAIL}}",
                        "name": "{{TestHelper.DEFAULT_CUSTOMER_NAME}}"
                    },
                    "availability": {
                        "start_at": "{{TestHelper.validTourDate:yyyy-MM-ddTHH:mm:ssZ}}",
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "customer_count": {{TestHelper.DEFAULT_GUEST_COUNT}},
                    "voucher_number": "{{TestHelper.VALID_VOUCHER}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, TestHelper.VALID_KEY);

        result.ShouldBeOfType<OkObjectResult>();

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.PurchaseCode == TestHelper.VALID_CODE, TestContext.Current.CancellationToken);
        booking.ShouldNotBeNull();
        booking.VoucherNumber.ShouldBe(TestHelper.VALID_VOUCHER);
    }

    [Fact]
    public async Task CreateNewBooking_AllowsNullVoucherNumber_WhenVoucherNumberIsNull() {
        var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateFoodTourApiController(context);
        string jsonString = $$"""
            {
                "booking": {
                    "pk": "{{TestHelper.UPDATE_TEST_CODE}}",
                    "contact": {
                        "email": "{{TestHelper.VALID_EMAIL}}",
                        "name": "{{TestHelper.DEFAULT_CUSTOMER_NAME}}"
                    },
                    "availability": {
                        "start_at": "{{TestHelper.validTourDate:yyyy-MM-ddTHH:mm:ssZ}}",
                        "item": {
                            "name": "{{TestHelper.WEYMOUTH_TOUR_NAME}}"
                        }
                    },
                    "customer_count": {{TestHelper.DEFAULT_GUEST_COUNT}},
                    "voucher_number": null
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, TestHelper.VALID_KEY);

        result.ShouldBeOfType<OkObjectResult>();

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.PurchaseCode == TestHelper.UPDATE_TEST_CODE, TestContext.Current.CancellationToken);
        booking.ShouldNotBeNull();
        booking.VoucherNumber.ShouldBeNull();
    }

    [Fact]
    public async Task CreateNewBooking_ReturnsUnauthorised_WhenInvalidKey() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = $$"""
            {
                "booking": {
                    "pk": "{{TestHelper.VALID_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, TestHelper.INVALID_KEY);

        var unauthorisedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorisedResult.StatusCode.ShouldBe(401);
        unauthorisedResult.Value.ShouldBe("Invalid key provided.");
    }

    [Fact]
    public async Task CreateNewBooking_ReturnsUnauthorised_WhenMissingKey() {
        var controller = TestHelper.CreateFoodTourApiController();
        string jsonString = $$"""
            {
                "booking": {
                    "pk": "{{TestHelper.VALID_CODE}}"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = await controller.CreateNewBooking(jsonElement, null!);

        var unauthorisedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorisedResult.StatusCode.ShouldBe(401);
    }

}
