namespace FoodTour.Test.Controllers;

public class HomeControllerQrImageTests : HomeControllerTestBase {
    [Fact]
    public async Task QrImage_ReturnsFileResult_WithPngContentType() {
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        string urlPath = $"SupplierVisit?qrtoken={booking.QrToken}";
        var sut = CreateConfiguredControllerWithUrl(context, urlPath);

        var result = await sut.QrImage(booking.QrToken);

        var fileResult = result.ShouldBeOfType<FileContentResult>();
        fileResult.ContentType.ShouldBe("image/png");
        fileResult.FileContents.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task QrImage_ReturnsBadRequest_WhenUrlIsNull() {
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredControllerWithUrl(context, null);

        var result = await sut.QrImage(booking.QrToken);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.ShouldNotBeNull();
        badRequestResult.Value.ShouldBe("Unable to generate QR code due to a system error.");
    }

    [Fact]
    public async Task QrImage_ReturnsBadRequest_WhenQrTokenIsInvalid() {
        var sut = CreateController();
        var emptyToken = Guid.Empty;

        var result = await sut.QrImage(emptyToken);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.ShouldNotBeNull();
        badRequestResult.Value.ShouldBe("QR Token is required.");
    }

    [Fact]
    public async Task QrImage_ReturnsNotFound_WhenBookingNotFound() {
        var sut = CreateController();
        var unknownToken = Guid.NewGuid();

        var result = await sut.QrImage(unknownToken);

        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(404);
        notFound.Value.ShouldBe("Booking not found or cancelled.");
    }

    [Fact]
    public async Task QrImage_ReturnsNotFound_WhenBookingIsCancelled() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.IsCancelled = true;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.QrImage(booking.QrToken);

        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(404);
        notFound.Value.ShouldBe("Booking not found or cancelled.");
    }
}