namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class BookingSelectionUiTests(FoodTourApplicationFixture fixture) {
    private const string BOOKING_CONFIRMATION_URL_PATTERN = "**/Home/BookingConfirmation?*";
    private const string BOOKING_CANCELLED_MESSAGE = "This booking has been cancelled.";
    private const string BOOKING_NOT_FOUND_MESSAGE = "We couldn't find your booking.";
    private const string FOOD_TOUR_CODE_PATH_TEMPLATE = "/Home/FoodTourCode?qrToken={0}";
    private const string FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE = "**/Home/FoodTourCode?qrToken={0}";
    private const string BOOKING_SUMMARY_PATH = "/Home/BookingSummary";
    private const string BOOKING_SUMMARY_URL_PATTERN = "**/Home/BookingSummary?*";
    private const string TOUR_CANCELLED_URL_PATTERN = "**/Home/TourCancelled?*";

    [Fact]
    public async Task CreateTour_NavigatesToBookingConfirmation_WhenBookingIsValidAndSuppliersAreSelected() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var booking = fixture.SeedData.BookingSelectionBooking;
        var selectedSuppliers = fixture.SeedData.ActiveSuppliers
            .Take(fixture.SeedData.RequiredSupplierSelectionCount)
            .ToList();
        var selectedSupplierNames = selectedSuppliers
            .Select(supplier => supplier.Name)
            .ToList();

        await sut.GotoAsync();
        await sut.EnterBookingDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);
        await sut.SelectSuppliersAsync(selectedSupplierNames);
        await sut.SubmitSelectionAsync();

        await page.WaitForURLAsync(BOOKING_SUMMARY_URL_PATTERN);
        page.Url.ShouldContain(BOOKING_SUMMARY_PATH);
        var bookingSummaryPage = new BookingSummaryPage(page);

        await Expect(bookingSummaryPage.SummaryHeading).ToBeVisibleAsync();

        foreach (var supplier in selectedSuppliers) {
            await Expect(bookingSummaryPage.SelectedSupplier(supplier.Name)).ToBeVisibleAsync();
        }

        await bookingSummaryPage.ConfirmBookingButton.ClickAsync();

        await page.WaitForURLAsync(BOOKING_CONFIRMATION_URL_PATTERN);
        var bookingConfirmationPage = new BookingConfirmationPage(page);

        await Expect(bookingConfirmationPage.BookingConfirmationHeading).ToBeVisibleAsync();
        await Expect(bookingConfirmationPage.ViewTourDetailsButton).ToBeVisibleAsync();
    }

    [Fact]
    public async Task EnterBooking_ShowsTourDetailsModal_WhenBookingIsAlreadyConfirmed() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var booking = fixture.SeedData.ConfirmedBooking;
        string expectedFoodTourCodePath = string.Format(FOOD_TOUR_CODE_PATH_TEMPLATE, booking.QrToken);
        string expectedFoodTourCodeUrlPattern = string.Format(FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE, booking.QrToken);

        await sut.GotoAsync();
        await sut.EnterConfirmedBookingDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);

        await Expect(sut.ViewYourTourDetailsButton).ToHaveAttributeAsync("href", expectedFoodTourCodePath);

        await sut.ViewYourTourDetailsButton.ClickAsync();

        await page.WaitForURLAsync(expectedFoodTourCodeUrlPattern);
        var foodTourQrCodePage = new FoodTourQRCodePage(page);

        await Expect(foodTourQrCodePage.TourSummaryHeading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task EnterBooking_ShowsWarning_WhenBookingCannotBeFound() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);

        await sut.GotoAsync();
        await sut.EnterBookingLookupDetailsAsync("missing.booking@example.com", "NOT-A-REAL-BOOKING");

        await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_NOT_FOUND_MESSAGE);
    }

    [Fact]
    public async Task EnterBooking_ShowsCancelledWarning_WhenBookingHasBeenCancelled() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var booking = fixture.SeedData.CancelledBooking;

        await sut.GotoAsync();
        await sut.EnterBookingLookupDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);

        await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_CANCELLED_MESSAGE);
    }

    [Fact]
    public async Task OpenFoodTourCode_RedirectsToTourCancelled_WhenBookingHasBeenCancelled() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var booking = fixture.SeedData.CancelledBooking;

        await page.GotoAsync(string.Format(FOOD_TOUR_CODE_PATH_TEMPLATE, booking.QrToken));

        await page.WaitForURLAsync(TOUR_CANCELLED_URL_PATTERN);
        page.Url.ShouldContain("/Home/TourCancelled");
        var tourCancelledPage = new TourCancelledPage(page);

        await Expect(tourCancelledPage.PageHeading).ToBeVisibleAsync();
        await Expect(tourCancelledPage.BookingDetails).ToContainTextAsync(booking.CustomerEmail);
        await Expect(tourCancelledPage.BookingDetails).ToContainTextAsync(booking.PurchaseCode);
    }

    [Fact]
    public async Task OpenFoodTourCode_ShowsCompletedState_WhenTourDateIsInThePast() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var booking = fixture.SeedData.PastBooking;

        await page.GotoAsync(string.Format(FOOD_TOUR_CODE_PATH_TEMPLATE, booking.QrToken));

        await page.WaitForURLAsync(string.Format(FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE, booking.QrToken));
        page.Url.ShouldContain("/Home/FoodTourCode");
        var foodTourQrCodePage = new FoodTourQRCodePage(page);

        await Expect(foodTourQrCodePage.TourCompleteHeading).ToBeVisibleAsync();
    }
}
