namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class BookingSelectionUiTests(FoodTourApplicationFixture fixture) : UiTestBase(fixture) {
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
    public Task CreateTour_NavigatesToBookingConfirmation_WhenBookingIsValidAndSuppliersAreSelected() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = Fixture.SeedData.BookingSelectionBooking;
            var selectedSuppliers = Fixture.SeedData.ActiveSuppliers
                .Take(Fixture.SeedData.RequiredSupplierSelectionCount)
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
        });
    }

    [Fact]
    public Task EnterBooking_ShowsTourDetailsModal_WhenBookingIsAlreadyConfirmed() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = Fixture.SeedData.ConfirmedBooking;
            string expectedFoodTourCodePath = string.Format(FOOD_TOUR_CODE_PATH_TEMPLATE, booking.QrToken);
            string expectedFoodTourCodeUrlPattern = string.Format(FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE, booking.QrToken);

            await sut.GotoAsync();
            await sut.EnterConfirmedBookingDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);

            await Expect(sut.ViewYourTourDetailsButton).ToHaveAttributeAsync("href", expectedFoodTourCodePath);

            await sut.ViewYourTourDetailsButton.ClickAsync();

            await page.WaitForURLAsync(expectedFoodTourCodeUrlPattern);
            var foodTourQrCodePage = new FoodTourQRCodePage(page);

            await Expect(foodTourQrCodePage.TourSummaryHeading).ToBeVisibleAsync();
        });
    }

    [Fact]
    public Task EnterBooking_ShowsWarning_WhenBookingCannotBeFound() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);

            await sut.GotoAsync();
            await sut.EnterBookingLookupDetailsAsync("missing.booking@example.com", "NOT-A-REAL-BOOKING");

            await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_NOT_FOUND_MESSAGE);
        });
    }

    [Fact]
    public Task EnterBooking_ShowsCancelledWarning_WhenBookingHasBeenCancelled() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = Fixture.SeedData.CancelledBooking;

            await sut.GotoAsync();
            await sut.EnterBookingLookupDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);

            await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_CANCELLED_MESSAGE);
        });
    }

    [Fact]
    public Task OpenFoodTourCode_RedirectsToTourCancelled_WhenBookingHasBeenCancelled() {
        return ExecutePageTestAsync(async page => {
            var booking = Fixture.SeedData.CancelledBooking;

            await page.GotoAsync(string.Format(FOOD_TOUR_CODE_PATH_TEMPLATE, booking.QrToken));

            await page.WaitForURLAsync(TOUR_CANCELLED_URL_PATTERN);
            page.Url.ShouldContain("/Home/TourCancelled");
            var tourCancelledPage = new TourCancelledPage(page);

            await Expect(tourCancelledPage.PageHeading).ToBeVisibleAsync();
            await Expect(tourCancelledPage.BookingDetails).ToContainTextAsync(booking.CustomerEmail);
            await Expect(tourCancelledPage.BookingDetails).ToContainTextAsync(booking.PurchaseCode);
        });
    }

    [Fact]
    public Task OpenFoodTourCode_ShowsCompletedState_WhenTourDateIsInThePast() {
        return ExecutePageTestAsync(async page => {
            var booking = Fixture.SeedData.PastBooking;

            await page.GotoAsync(string.Format(FOOD_TOUR_CODE_PATH_TEMPLATE, booking.QrToken));

            await page.WaitForURLAsync(string.Format(FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE, booking.QrToken));
            page.Url.ShouldContain("/Home/FoodTourCode");
            var foodTourQrCodePage = new FoodTourQRCodePage(page);

            await Expect(foodTourQrCodePage.TourCompleteHeading).ToBeVisibleAsync();
        });
    }

    [Fact]
    public Task SelectSuppliers_LeavesCreateTourDisabled_WhenFewerThanFiveSuppliersAreSelected() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = await CreateTemporaryBookingAsync(DateTime.SpecifyKind(DateTime.Today.AddDays(14).AddHours(11), DateTimeKind.Utc));
            var supplierNames = Fixture.SeedData.ActiveSuppliers
                .Take(Fixture.SeedData.RequiredSupplierSelectionCount - 1)
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
        });
    }

    [Fact]
    public Task SelectSuppliers_PreventsSelectingASixthSupplier_WhenMaximumSelectionsAreReached() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = await CreateTemporaryBookingAsync(DateTime.SpecifyKind(DateTime.Today.AddDays(14).AddHours(11), DateTimeKind.Utc));
            var supplierNames = Fixture.SeedData.ActiveSuppliers
                .Take(Fixture.SeedData.RequiredSupplierSelectionCount + 1)
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
                selectedSupplierCount.ShouldBe(Fixture.SeedData.RequiredSupplierSelectionCount);
            } finally {
                await DeleteTemporaryBookingAsync(booking.Id);
            }
        });
    }

    [Fact]
    public Task EnterBookingLookupDetails_DoesNotValidate_WhenEmailFormatIsInvalid() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);

            await sut.GotoAsync();
            await sut.EnterBookingLookupDetailsAsync("not-an-email", "123123123");
            await page.WaitForTimeoutAsync(700);

            await Expect(sut.BookingStatusMessage).ToBeHiddenAsync();
            await Expect(sut.PartnerConfirmationPrompt).ToBeHiddenAsync();
        });
    }

    [Fact]
    public Task ClearBookingLookup_RestoresOpeningTimes_WhenPreviouslyValidatedBookingIsCleared() {
        return ExecutePageTestAsync(async page => {
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
        });
    }

    [Fact]
    public Task ConfirmBookingSummary_RedirectsToConfirmation_WhenBookingIsAlreadyConfirmed() {
        return ExecutePageTestAsync(async page => {
            var booking = Fixture.SeedData.ConfirmedBooking;

            await page.GotoAsync(string.Format(BOOKING_SUMMARY_PATH_TEMPLATE, booking.QrToken));
            var bookingSummaryPage = new BookingSummaryPage(page);

            await Expect(bookingSummaryPage.SummaryHeading).ToBeVisibleAsync();
            await bookingSummaryPage.ConfirmBookingButton.ClickAsync();

            await page.WaitForURLAsync(BOOKING_CONFIRMATION_URL_PATTERN);
            var bookingConfirmationPage = new BookingConfirmationPage(page);

            await Expect(bookingConfirmationPage.BookingConfirmationHeading).ToBeVisibleAsync();
        });
    }

    [Fact]
    public Task ConfirmBooking_ShowsWarning_WhenConfirmationEmailFailsToSend() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = await CreateTemporaryBookingAsync(DateTime.SpecifyKind(DateTime.Today.AddDays(14).AddHours(11), DateTimeKind.Utc));
            var supplierNames = Fixture.SeedData.ActiveSuppliers
                .Take(Fixture.SeedData.RequiredSupplierSelectionCount)
                .Select(supplier => supplier.Name)
                .ToList();

            Fixture.TestEmailSender.Clear();
            Fixture.TestEmailSender.ThrowOnSend = true;

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
                Fixture.TestEmailSender.ThrowOnSend = false;
                await DeleteTemporaryBookingAsync(booking.Id);
            }
        });
    }

    [Fact]
    public Task GotoIndex_PrefillsBookingFields_WhenQrTokenIsValid() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = Fixture.SeedData.CancelledBooking;

            await sut.GotoAsync(booking.QrToken);

            await Expect(sut.CustomerEmailInput).ToHaveValueAsync(booking.CustomerEmail);
            await Expect(sut.PurchaseCodeInput).ToHaveValueAsync(booking.PurchaseCode);
            await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_CANCELLED_MESSAGE);
        });
    }

    [Fact]
    public Task GotoIndex_LeavesBookingFieldsBlank_WhenQrTokenIsInvalid() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);

            await page.GotoAsync(INVALID_QR_TOKEN_PATH);
            await Expect(sut.PageHeading).ToBeVisibleAsync();

            await Expect(sut.CustomerEmailInput).ToHaveValueAsync(string.Empty);
            await Expect(sut.PurchaseCodeInput).ToHaveValueAsync(string.Empty);
            await Expect(sut.BookingStatusMessage).ToBeHiddenAsync();
        });
    }

    [Fact]
    public Task SubmitSelection_ShowsBookingReferenceValidation_WhenLookupHasNotValidatedBooking() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var supplierNames = Fixture.SeedData.ActiveSuppliers
                .Take(Fixture.SeedData.RequiredSupplierSelectionCount)
                .Select(supplier => supplier.Name)
                .ToList();

            await sut.GotoAsync();
            await sut.CustomerEmailInput.FillAsync("person@example.com");
            foreach (string supplierName in supplierNames) {
                await sut.ToggleSupplierAsync(supplierName);
            }
            await sut.SubmitSelectionAsync();

            await Expect(page.GetByText(BOOKING_REFERENCE_REQUIRED_MESSAGE)).ToBeVisibleAsync();
        });
    }

    private async Task<TemporaryBookingData> CreateTemporaryBookingAsync(DateTime tourDate) {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(Fixture.ConnectionString);

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
        optionsBuilder.UseSqlServer(Fixture.ConnectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        var booking = await context.Bookings.SingleAsync(existingBooking => existingBooking.Id == bookingId, TestContext.Current.CancellationToken);
        context.Bookings.Remove(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed record TemporaryBookingData(int Id, string CustomerEmail, string PurchaseCode);
}
