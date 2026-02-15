namespace FoodTour.Test.Controllers;

public class HomeControllerSaveConfirmedBookingTests : HomeControllerTestBase {
    [Fact]
    public async Task SaveConfirmedBooking_ReturnsBadRequest_WhenBookingInvalidAsync() {
        var sut = CreateController();

        var result = await sut.SaveConfirmedBooking();

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid request");
    }

    [Fact]
    public async Task SaveConfirmedBooking_ReturnsBadRequest_WhenInvalidSuppliersAsync() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateConfiguredController(context);
        var confirmation = new BookingConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2]
        };

        var result = await sut.SaveConfirmedBooking(confirmation);

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(400);
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString()!.ShouldContain("Invalid supplier selection");
    }

    [Fact]
    public async Task SaveConfirmedBooking_SavesBookingAndReturnsOkAsync() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        string urlPath = $"FoodTourCode?qrtoken={new Guid()}";
        var sut = CreateConfiguredControllerWithUrl(context, urlPath);
        var confirmation = new BookingConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };
        sut.TempData["Confirmation"] = JsonSerializer.Serialize(confirmation);

        var result = await sut.SaveConfirmedBooking(confirmation);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        okResult.Value.ShouldNotBeNull();
        okResult.Value.ToString()!.ShouldContain("True");

        var bookings = context.Bookings.Include(b => b.BookingSuppliers).ToList();
        bookings.Count.ShouldBe(1);

        var booking = bookings[0];
        booking.CustomerEmail.ShouldBe(confirmation.CustomerEmail);
        booking.PurchaseCode.ShouldBe(confirmation.PurchaseCode);
        booking.TourDate.ShouldBe(confirmation.TourDate);
        booking.BookingSuppliers.Count.ShouldBe(5);
        booking.BookingSuppliers.Select(bs => bs.SupplierId).ShouldBe(confirmation.SelectedSupplierIds);
    }

    [Fact]
    public async Task SaveConfirmedBooking_CreatesVisitRecordsForEachSelectedSupplier_WhenBookingIsConfirmedAsync() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        string urlPath = $"FoodTourCode?qrtoken={new Guid()}";
        var sut = CreateConfiguredControllerWithUrl(context, urlPath);

        var confirmation = new BookingConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.SaveConfirmedBooking(confirmation);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);

        var visits = await context.Visits.ToListAsync();
        visits.Count.ShouldBe(5);

        var booking = await context.Bookings.FirstOrDefaultAsync();
        booking.ShouldNotBeNull();

        foreach (int supplierId in confirmation.SelectedSupplierIds) {
            var visit = visits.FirstOrDefault(v => v.SupplierId == supplierId);
            visit.ShouldNotBeNull();
            visit.BookingId.ShouldBe(booking.Id);
            visit.IsClaimed.ShouldBeFalse();
            visit.ClaimedAt.ShouldBeNull();
        }
    }

    [Fact]
    public async Task SaveConfirmedBooking_CreatesVisitsWithCorrectDefaultValues() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredController(context);
        var confirmation = new BookingConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        await sut.SaveConfirmedBooking(confirmation);

        var visits = await context.Visits.ToListAsync();
        visits.ShouldAllBe(v => v.IsClaimed == false);
        visits.ShouldAllBe(v => v.ClaimedAt == null);
    }

    [Fact]
    public async Task SaveConfirmedBooking_ReturnsServerError_WhenDatabaseSaveThrowsAsync() {
        using var context = TestHelper.CreateContextWithSuppliersFailingOnSave();
        var sut = CreateConfiguredController(context);

        var confirmation = new BookingConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.SaveConfirmedBooking(confirmation);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(500);
        objectResult.Value.ShouldNotBeNull();
        objectResult.Value.ToString()!.ShouldContain("BookingSummary");
        objectResult.Value.ToString()!.ShouldContain("due to a system error");
    }

    [Fact]
    public async Task SaveConfirmedBooking_ReturnsOk_WhenSaveEmailThrowsAsync() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredControllerWithUrl(context, null);
        var confirmation = new BookingConfirmationVM {
            TourDate = TestHelper.validTourDate,
            CustomerEmail = TestHelper.VALID_EMAIL,
            PurchaseCode = TestHelper.VALID_CODE,
            SelectedSupplierIds = [1, 2, 3, 4, 5]
        };

        var result = await sut.SaveConfirmedBooking(confirmation);
        
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        okResult.Value.ShouldNotBeNull();
        okResult.Value.ToString()!.ShouldContain("Confirmation");
        sut.TempData.ContainsKey("BookingEmailError").ShouldBeTrue();
    }
}