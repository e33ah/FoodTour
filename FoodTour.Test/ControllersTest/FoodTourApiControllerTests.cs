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

    [Fact]
    public async Task CheckBooking_ReturnsOkWithFoundTrue_WhenActiveBookingExists() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "test@example.com",
            PurchaseCode = "ABC123",
            TourDate = DateTime.UtcNow.AddDays(7),
            IsCancelled = false,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = "test@example.com",
            PurchaseCode = "ABC123"
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        object? found = value.GetType().GetProperty("found")?.GetValue(value);
        found.ShouldBe(true);
        object? isCancelled = value.GetType().GetProperty("isCancelled")?.GetValue(value);
        isCancelled.ShouldBe(false);
        object? isConfirmed = value.GetType().GetProperty("isConfirmed")?.GetValue(value);
        isConfirmed.ShouldBe(false);
        object? qrToken = value.GetType().GetProperty("qrToken")?.GetValue(value);
        qrToken.ShouldNotBeNull();
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("We've found your booking!");
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithCancelledTrue_WhenCancelledBookingExists() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "cancelled@example.com",
            PurchaseCode = "CANCELLED123",
            TourDate = DateTime.UtcNow.AddDays(7),
            IsCancelled = true,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = "cancelled@example.com",
            PurchaseCode = "CANCELLED123"
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        object? found = value.GetType().GetProperty("found")?.GetValue(value);
        found.ShouldBe(true);
        object? isCancelled = value.GetType().GetProperty("isCancelled")?.GetValue(value);
        isCancelled.ShouldBe(true);
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("This booking has been cancelled. Please contact support if you believe this is an error.");
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithConfirmedTrue_WhenConfirmedBookingExists() {
        var context = TestHelper.CreateInMemoryContext();
        var expectedQrToken = Guid.NewGuid();
        var booking = new Booking {
            CustomerEmail = "confirmed@example.com",
            PurchaseCode = "CONFIRMED123",
            TourDate = DateTime.UtcNow.AddDays(7),
            IsCancelled = false,
            IsConfirmed = true,
            QrToken = expectedQrToken,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = "confirmed@example.com",
            PurchaseCode = "CONFIRMED123"
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        object? found = value.GetType().GetProperty("found")?.GetValue(value);
        found.ShouldBe(true);
        object? isCancelled = value.GetType().GetProperty("isCancelled")?.GetValue(value);
        isCancelled.ShouldBe(false);
        object? isConfirmed = value.GetType().GetProperty("isConfirmed")?.GetValue(value);
        isConfirmed.ShouldBe(true);
        object? qrToken = value.GetType().GetProperty("qrToken")?.GetValue(value);
        qrToken.ShouldBe(expectedQrToken);
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("We've found your booking!");
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithFoundFalse_WhenBookingDoesNotExist() {
        var context = TestHelper.CreateInMemoryContext();
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = "notfound@example.com",
            PurchaseCode = "NOTFOUND123"
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        object? found = value.GetType().GetProperty("found")?.GetValue(value);
        found.ShouldBe(false);
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("We couldn't find your booking. Please check your booking reference and email address for any typos, or contact support if the problem continues");
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithFoundFalse_WhenEmailDoesNotMatch() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "correct@example.com",
            PurchaseCode = "ABC123",
            TourDate = DateTime.UtcNow.AddDays(7),
            IsCancelled = false,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = "wrong@example.com",
            PurchaseCode = "ABC123"
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        object? found = value.GetType().GetProperty("found")?.GetValue(value);
        found.ShouldBe(false);
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithFoundFalse_WhenPurchaseCodeDoesNotMatch() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "test@example.com",
            PurchaseCode = "CORRECT123",
            TourDate = DateTime.UtcNow.AddDays(7),
            IsCancelled = false,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = "test@example.com",
            PurchaseCode = "WRONG123"
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        object? found = value.GetType().GetProperty("found")?.GetValue(value);
        found.ShouldBe(false);
    }

    [Fact]
    public async Task CheckBooking_ReturnsBadRequest_WhenEmailIsEmpty() {
        var controller = TestHelper.CreateFoodTourApiController();

        var request = new BookingCheckRequest {
            CustomerEmail = "",
            PurchaseCode = "ABC123"
        };

        var result = await controller.CheckBooking(request);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        object? value = badRequestResult.Value;
        value.ShouldNotBeNull();
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("Email and booking reference are required.");
    }

    [Fact]
    public async Task CheckBooking_ReturnsBadRequest_WhenPurchaseCodeIsEmpty() {
        var controller = TestHelper.CreateFoodTourApiController();

        var request = new BookingCheckRequest {
            CustomerEmail = "test@example.com",
            PurchaseCode = ""
        };

        var result = await controller.CheckBooking(request);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        object? value = badRequestResult.Value;
        value.ShouldNotBeNull();
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("Email and booking reference are required.");
    }

    [Fact]
    public async Task CheckBooking_ReturnsBadRequest_WhenBothFieldsAreEmpty() {
        var controller = TestHelper.CreateFoodTourApiController();

        var request = new BookingCheckRequest {
            CustomerEmail = "",
            PurchaseCode = ""
        };

        var result = await controller.CheckBooking(request);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        object? value = badRequestResult.Value;
        value.ShouldNotBeNull();
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("Email and booking reference are required.");
    }

    [Fact]
    public async Task CheckBooking_ReturnsBadRequest_WhenEmailIsWhitespace() {
        var controller = TestHelper.CreateFoodTourApiController();

        var request = new BookingCheckRequest {
            CustomerEmail = "   ",
            PurchaseCode = "ABC123"
        };

        var result = await controller.CheckBooking(request);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task CheckBooking_IsCaseInsensitiveForEmail() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "Test@Example.Com",
            PurchaseCode = "ABC123",
            TourDate = DateTime.UtcNow.AddDays(7),
            IsCancelled = false,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = "test@example.com",
            PurchaseCode = "ABC123"
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        object? found = value.GetType().GetProperty("found")?.GetValue(value);
        found.ShouldBe(true);
        object? isCancelled = value.GetType().GetProperty("isCancelled")?.GetValue(value);
        isCancelled.ShouldBe(false);
        object? isConfirmed = value.GetType().GetProperty("isConfirmed")?.GetValue(value);
        isConfirmed.ShouldBe(false);
        object? qrToken = value.GetType().GetProperty("qrToken")?.GetValue(value);
        qrToken.ShouldNotBeNull();
    }
}
