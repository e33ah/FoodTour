namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class BookingSelectionUiTests(FoodTourApplicationFixture fixture) {
    private const string BOOKING_CONFIRMATION_URL_PATTERN = "**/Home/BookingConfirmation?*";
    private const string BOOKING_CANCELLED_MESSAGE = "This booking has been cancelled.";
    private const string BOOKING_NOT_FOUND_MESSAGE = "We couldn't find your booking.";
    private const string BOOKING_REFERENCE_REQUIRED_MESSAGE = "Please enter your booking reference to validate your booking.";
    private const string BOOKING_SUMMARY_PATH = "/Home/BookingSummary";
    private const string BOOKING_SUMMARY_PATH_TEMPLATE = "/Home/BookingSummary?qrToken={0}";
    private const string BOOKING_SUMMARY_URL_PATTERN = "**/Home/BookingSummary?*";
    private const string EMAIL_WARNING_MESSAGE = "Your booking was saved, but we were unable to send a confirmation email.";
    private const string FOOD_TOUR_CODE_PATH_TEMPLATE = "/Home/FoodTourCode?qrToken={0}";
    private const string FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE = "**/Home/FoodTourCode?qrToken={0}";
    private const string GENERAL_OPENING_TIMES = "Monday-Friday: 8am - 5pm; Saturday: 9am - 4pm; Sunday: 9am - 3pm";
    private const string INVALID_QR_TOKEN_PATH = "/?qrToken=11111111-1111-1111-1111-111111111111";
    private static readonly DateTime lookupResetTourDate = new(2035, 6, 11, 11, 15, 0, DateTimeKind.Utc);
    private const string MONDAY_OPENING_TIMES = "8am - 5pm";
    private const string OPENING_TIMES_SUPPLIER = "Coffee and Cake";
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

    [Fact]
    public async Task SelectSuppliers_LeavesCreateTourDisabled_WhenFewerThanFiveSuppliersAreSelected() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var booking = await CreateTemporaryBookingAsync(DateTime.SpecifyKind(DateTime.Today.AddDays(14).AddHours(11), DateTimeKind.Utc));
        var supplierNames = fixture.SeedData.ActiveSuppliers
            .Take(fixture.SeedData.RequiredSupplierSelectionCount - 1)
            .Select(supplier => supplier.Name)
            .ToList();

        try {
            await sut.GotoAsync();
            await sut.EnterBookingDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);

            foreach (string supplierName in supplierNames) {
                await sut.ToggleSupplierAsync(supplierName);
            }

            await Expect(sut.CreateTourButton).ToBeDisabledAsync();
        } finally {
            await DeleteTemporaryBookingAsync(booking.Id);
        }
    }

    [Fact]
    public async Task SelectSuppliers_PreventsSelectingASixthSupplier_WhenMaximumSelectionsAreReached() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var booking = await CreateTemporaryBookingAsync(DateTime.SpecifyKind(DateTime.Today.AddDays(14).AddHours(11), DateTimeKind.Utc));
        var supplierNames = fixture.SeedData.ActiveSuppliers
            .Take(fixture.SeedData.RequiredSupplierSelectionCount + 1)
            .Select(supplier => supplier.Name)
            .ToList();

        try {
            await sut.GotoAsync();
            await sut.EnterBookingDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);

            foreach (string supplierName in supplierNames) {
                await sut.ToggleSupplierAsync(supplierName);
            }

            await Expect(sut.MaxSelectionMessage).ToBeVisibleAsync();
            int selectedSupplierCount = await sut.GetSelectedSupplierCountAsync();
            selectedSupplierCount.ShouldBe(fixture.SeedData.RequiredSupplierSelectionCount);
        } finally {
            await DeleteTemporaryBookingAsync(booking.Id);
        }
    }

    [Fact]
    public async Task EnterBookingLookupDetails_DoesNotValidate_WhenEmailFormatIsInvalid() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);

        await sut.GotoAsync();
        await sut.EnterBookingLookupDetailsAsync("not-an-email", "123123123");
        await page.WaitForTimeoutAsync(700);

        await Expect(sut.BookingStatusMessage).ToBeHiddenAsync();
        await Expect(sut.PartnerConfirmationPrompt).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ClearBookingLookup_RestoresOpeningTimes_WhenPreviouslyValidatedBookingIsCleared() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var booking = await CreateTemporaryBookingAsync(lookupResetTourDate);

        try {
            await sut.GotoAsync();
            await Expect(sut.GetSupplierOpeningTimes(OPENING_TIMES_SUPPLIER)).ToContainTextAsync(GENERAL_OPENING_TIMES);

            await sut.EnterBookingDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);
            await Expect(sut.GetSupplierOpeningTimes(OPENING_TIMES_SUPPLIER)).ToContainTextAsync(MONDAY_OPENING_TIMES);

            await sut.PurchaseCodeInput.FillAsync(string.Empty);

            await Expect(sut.BookingStatusMessage).ToBeHiddenAsync();
            await Expect(sut.GetSupplierOpeningTimes(OPENING_TIMES_SUPPLIER)).ToContainTextAsync(GENERAL_OPENING_TIMES);
        } finally {
            await DeleteTemporaryBookingAsync(booking.Id);
        }
    }

    [Fact]
    public async Task ConfirmBookingSummary_RedirectsToConfirmation_WhenBookingIsAlreadyConfirmed() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var booking = fixture.SeedData.ConfirmedBooking;

        await page.GotoAsync(string.Format(BOOKING_SUMMARY_PATH_TEMPLATE, booking.QrToken));
        var bookingSummaryPage = new BookingSummaryPage(page);

        await Expect(bookingSummaryPage.SummaryHeading).ToBeVisibleAsync();
        await bookingSummaryPage.ConfirmBookingButton.ClickAsync();

        await page.WaitForURLAsync(BOOKING_CONFIRMATION_URL_PATTERN);
        var bookingConfirmationPage = new BookingConfirmationPage(page);

        await Expect(bookingConfirmationPage.BookingConfirmationHeading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ConfirmBooking_ShowsWarning_WhenConfirmationEmailFailsToSend() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var booking = await CreateTemporaryBookingAsync(DateTime.SpecifyKind(DateTime.Today.AddDays(14).AddHours(11), DateTimeKind.Utc));
        var supplierNames = fixture.SeedData.ActiveSuppliers
            .Take(fixture.SeedData.RequiredSupplierSelectionCount)
            .Select(supplier => supplier.Name)
            .ToList();

        fixture.TestEmailSender.Clear();
        fixture.TestEmailSender.ThrowOnSend = true;

        try {
            await sut.GotoAsync();
            await sut.EnterBookingDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);
            await sut.SelectSuppliersAsync(supplierNames);
            await sut.SubmitSelectionAsync();

            await page.WaitForURLAsync(BOOKING_SUMMARY_URL_PATTERN);
            var bookingSummaryPage = new BookingSummaryPage(page);
            await bookingSummaryPage.ConfirmBookingButton.ClickAsync();

            await page.WaitForURLAsync(BOOKING_CONFIRMATION_URL_PATTERN);
            var bookingConfirmationPage = new BookingConfirmationPage(page);

            await Expect(bookingConfirmationPage.BookingConfirmationWarning).ToContainTextAsync(EMAIL_WARNING_MESSAGE);
        } finally {
            fixture.TestEmailSender.ThrowOnSend = false;
            await DeleteTemporaryBookingAsync(booking.Id);
        }
    }

    [Fact]
    public async Task GotoIndex_PrefillsBookingFields_WhenQrTokenIsValid() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var booking = fixture.SeedData.CancelledBooking;

        await sut.GotoAsync(booking.QrToken);

        await Expect(sut.CustomerEmailInput).ToHaveValueAsync(booking.CustomerEmail);
        await Expect(sut.PurchaseCodeInput).ToHaveValueAsync(booking.PurchaseCode);
        await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_CANCELLED_MESSAGE);
    }

    [Fact]
    public async Task GotoIndex_LeavesBookingFieldsBlank_WhenQrTokenIsInvalid() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);

        await page.GotoAsync(INVALID_QR_TOKEN_PATH);
        await Expect(sut.PageHeading).ToBeVisibleAsync();

        await Expect(sut.CustomerEmailInput).ToHaveValueAsync(string.Empty);
        await Expect(sut.PurchaseCodeInput).ToHaveValueAsync(string.Empty);
        await Expect(sut.BookingStatusMessage).ToBeHiddenAsync();
    }

    [Fact]
    public async Task SubmitSelection_ShowsBookingReferenceValidation_WhenLookupHasNotValidatedBooking() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new BookingSelectionPage(page);
        var supplierNames = fixture.SeedData.ActiveSuppliers
            .Take(fixture.SeedData.RequiredSupplierSelectionCount)
            .Select(supplier => supplier.Name)
            .ToList();

        await sut.GotoAsync();
        await sut.CustomerEmailInput.FillAsync("person@example.com");
        foreach (string supplierName in supplierNames) {
            await sut.ToggleSupplierAsync(supplierName);
        }
        await sut.SubmitSelectionAsync();

        await Expect(page.GetByText(BOOKING_REFERENCE_REQUIRED_MESSAGE)).ToBeVisibleAsync();
    }

    private async Task<TemporaryBookingData> CreateTemporaryBookingAsync(DateTime tourDate) {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(fixture.ConnectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        var qrToken = Guid.NewGuid();
        string purchaseCode = Guid.NewGuid().ToString("N")[..9];
        var booking = new Booking {
            TourDate = tourDate,
            CustomerEmail = $"ui-{purchaseCode}@example.com",
            PurchaseCode = purchaseCode,
            VoucherNumber = null,
            SelectedSupplierIds = [],
            CreatedAt = DateTime.SpecifyKind(DateTime.Today.AddDays(-1).AddHours(9), DateTimeKind.Utc),
            TotalGuests = 2,
            CustomerName = "UI Test Booking",
            QrToken = qrToken,
            IsConfirmed = false,
            IsCancelled = false,
            RequiresEmailConfirmation = false
        };

        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new TemporaryBookingData(booking.Id, booking.CustomerEmail, booking.PurchaseCode);
    }

    private async Task DeleteTemporaryBookingAsync(int bookingId) {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(fixture.ConnectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        var booking = await context.Bookings.SingleAsync(existingBooking => existingBooking.Id == bookingId, TestContext.Current.CancellationToken);
        context.Bookings.Remove(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed record TemporaryBookingData(int Id, string CustomerEmail, string PurchaseCode);
}
