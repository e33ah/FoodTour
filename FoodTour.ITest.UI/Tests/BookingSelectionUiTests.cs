namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class BookingSelectionUiTests(FoodTourApplicationFixture fixture) {
    private const string BOOKING_CONFIRMATION_URL_PATTERN = "**/Home/BookingConfirmation?*";
    private const string FOOD_TOUR_CODE_PATH_TEMPLATE = "/Home/FoodTourCode?qrToken={0}";
    private const string FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE = "**/Home/FoodTourCode?qrToken={0}";
    private const string BOOKING_SUMMARY_PATH = "/Home/BookingSummary";
    private const string BOOKING_SUMMARY_URL_PATTERN = "**/Home/BookingSummary?*";

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
        await Expect(sut.SummaryHeading).ToBeVisibleAsync();

        foreach (var supplier in selectedSuppliers) {
            await Expect(sut.SelectedSupplier(supplier.Name)).ToBeVisibleAsync();
        }

        await sut.ConfirmBookingButton.ClickAsync();

        await page.WaitForURLAsync(BOOKING_CONFIRMATION_URL_PATTERN);
        await Expect(sut.BookingConfirmationHeading).ToBeVisibleAsync();
        await Expect(sut.ViewTourDetailsButton).ToBeVisibleAsync();
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
        await Expect(sut.TourSummaryHeading).ToBeVisibleAsync();
    }

}
