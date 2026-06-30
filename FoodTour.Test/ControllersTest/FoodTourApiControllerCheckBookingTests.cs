namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerCheckBookingTests : TestBase {
    [Fact]
    public async Task CheckBooking_ReturnsOkWithFoundTrue_WhenActiveBookingExists() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            IsCancelled = false,
            SelectedSupplierIds = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS)
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE
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
        object? tourDate = value.GetType().GetProperty("tourDate")?.GetValue(value);
        tourDate.ShouldBe(TestHelper.validTourDate);
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("We've found your booking!");
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithCancelledTrue_WhenCancelledBookingExists() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = TestHelper.ALTERNATIVE_EMAIL,
            PurchaseCode = TestHelper.CANCEL_TEST_CODE,
            TourDate = TestHelper.validTourDate,
            IsCancelled = true,
            SelectedSupplierIds = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS)
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = TestHelper.ALTERNATIVE_EMAIL,
            PurchaseCode = TestHelper.CANCEL_TEST_CODE
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
        object? tourDate = value.GetType().GetProperty("tourDate")?.GetValue(value);
        tourDate.ShouldBe(TestHelper.validTourDate);
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("This booking has been cancelled. Please contact support if you believe this is an error.");
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithConfirmedTrue_WhenConfirmedBookingExists() {
        var context = TestHelper.CreateInMemoryContext();
        var expectedQrToken = Guid.NewGuid();
        var booking = new Booking {
            CustomerEmail = TestHelper.UPDATED_EMAIL,
            PurchaseCode = TestHelper.UPDATE_TEST_CODE,
            TourDate = TestHelper.validTourDate,
            IsCancelled = false,
            IsConfirmed = true,
            QrToken = expectedQrToken,
            SelectedSupplierIds = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS)
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = TestHelper.UPDATED_EMAIL,
            PurchaseCode = TestHelper.UPDATE_TEST_CODE
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
        object? tourDate = value.GetType().GetProperty("tourDate")?.GetValue(value);
        tourDate.ShouldBe(TestHelper.validTourDate);
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
        object? tourDate = value.GetType().GetProperty("tourDate")?.GetValue(value);
        tourDate.ShouldBeNull();
        string? message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        message.ShouldBe("We couldn't find your booking. Please check your booking reference and email address for any typos, or contact support if the problem continues");
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithFoundFalse_WhenEmailDoesNotMatch() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            IsCancelled = false,
            SelectedSupplierIds = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS)
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = TestHelper.ALTERNATIVE_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE
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
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            IsCancelled = false,
            SelectedSupplierIds = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS)
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.CANCEL_TEST_CODE
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
    public async Task CheckBooking_ReturnsOkWithFoundTrue_WhenVoucherNumberMatchesAfterPurchaseCodeLookupFails() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            VoucherNumber = TestHelper.VALID_VOUCHER,
            TourDate = TestHelper.validTourDate,
            IsCancelled = false,
            SelectedSupplierIds = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS)
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_VOUCHER
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
    }

    [Fact]
    public async Task CheckBooking_ReturnsPartnerConfirmationResponse_WhenVoucherMatchesAndEmailDoesNotMatchPendingBooking() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "pseudo@partner.test",
            PurchaseCode = TestHelper.VALID_CODE,
            VoucherNumber = TestHelper.VALID_VOUCHER,
            TourDate = TestHelper.validTourDate,
            RequiresEmailConfirmation = true
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_VOUCHER
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        value.GetType().GetProperty("found")?.GetValue(value).ShouldBe(false);
        value.GetType().GetProperty("requiresPartnerConfirmation")?.GetValue(value).ShouldBe(true);
    }

    [Fact]
    public async Task CheckBooking_ReturnsOkWithFoundTrue_WhenVoucherMatchesPendingBookingAndEmailMatchesCaseInsensitively() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = new Booking {
            CustomerEmail = "Customer@Partner.Test",
            PurchaseCode = TestHelper.VALID_CODE,
            VoucherNumber = TestHelper.VALID_VOUCHER,
            TourDate = TestHelper.validTourDate,
            RequiresEmailConfirmation = true
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = "customer@partner.test",
            PurchaseCode = TestHelper.VALID_VOUCHER
        };

        var result = await controller.CheckBooking(request);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        object? value = okResult.Value;
        value.ShouldNotBeNull();
        value.GetType().GetProperty("found")?.GetValue(value).ShouldBe(true);
        value.GetType().GetProperty("isCancelled")?.GetValue(value).ShouldBe(false);
        value.GetType().GetProperty("requiresPartnerConfirmation")?.GetValue(value).ShouldBeNull();
    }

    [Fact]
    public async Task CheckBooking_ReturnsBadRequest_WhenEmailIsEmpty() {
        var controller = TestHelper.CreateFoodTourApiController();

        var request = new BookingCheckRequest {
            CustomerEmail = "",
            PurchaseCode = TestHelper.VALID_CODE
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
            CustomerEmail = TestHelper.VALID_EMAIL,
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
            PurchaseCode = TestHelper.VALID_CODE
        };

        var result = await controller.CheckBooking(request);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task CheckBooking_IsCaseInsensitiveForEmail() {
        var context = TestHelper.CreateInMemoryContext();
        string mixedCaseEmail = "Customer@Test.Com";
        var booking = new Booking {
            CustomerEmail = mixedCaseEmail,
            PurchaseCode = TestHelper.VALID_CODE,
            TourDate = TestHelper.validTourDate,
            IsCancelled = false,
            SelectedSupplierIds = TestHelper.CreateSupplierIdList(TestHelper.EXPECTED_MAX_SELECTIONS)
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var controller = TestHelper.CreateFoodTourApiController(context);

        var request = new BookingCheckRequest {
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE
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
