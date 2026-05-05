namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class BookingSelectionUiTests(FoodTourApplicationFixture fixture) : UiTestBase(fixture) {
    private const string BOOKING_CONFIRMATION_URL_PATTERN = "**/Home/BookingConfirmation?*";
    private const string BOOKING_CANCELLED_MESSAGE = "This booking has been cancelled.";
    private const string BOOKING_FOUND_MESSAGE = "We've found your booking!";
    private const string BOOKING_NOT_FOUND_MESSAGE = "We couldn't find your booking.";
    private const string BOOKING_REFERENCE_REQUIRED_MESSAGE = "Please enter your booking reference to validate your booking.";
    private const string BOOKING_SUMMARY_PATH = "/Home/BookingSummary";
    private const string BOOKING_SUMMARY_PATH_TEMPLATE = "/Home/BookingSummary?qrToken={0}";
    private const string BOOKING_SUMMARY_URL_PATTERN = "**/Home/BookingSummary?*";
    private const string EMAIL_WARNING_MESSAGE = "Your booking was saved, but we were unable to send a confirmation email.";
    private const string GENERAL_OPENING_TIMES = "Monday-Friday: 8am - 5pm; Saturday: 9am - 4pm; Sunday: 9am - 3pm";
    private const string INVALID_QR_TOKEN_PATH = "/?qrToken=11111111-1111-1111-1111-111111111111";
    private static readonly DateTime lookupResetTourDate = new(2035, 6, 11, 11, 15, 0, DateTimeKind.Utc);
    private const string MONDAY_OPENING_TIMES = "8am - 5pm";
    private const string OPENING_TIMES_SUPPLIER = "Coffee and Cake";

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
            string expectedFoodTourCodePath = string.Format("/Home/FoodTourCode?qrToken={0}", booking.QrToken);
            string expectedFoodTourCodeUrlPattern = string.Format("**/Home/FoodTourCode?qrToken={0}", booking.QrToken);

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
    public Task EnterBooking_ClearsWarningAndAllowsProgress_WhenLookupBecomesValidAfterInvalidAttempt() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = Fixture.SeedData.BookingSelectionBooking;
            var selectedSupplierNames = Fixture.SeedData.ActiveSuppliers
                .Take(Fixture.SeedData.RequiredSupplierSelectionCount)
                .Select(supplier => supplier.Name)
                .ToList();

            await sut.GotoAsync();
            await sut.EnterBookingLookupDetailsAsync("missing.booking@example.com", "NOT-A-REAL-BOOKING");
            await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_NOT_FOUND_MESSAGE);

            await sut.EnterBookingLookupDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);
            await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_FOUND_MESSAGE);
            await Expect(sut.PartnerConfirmationPrompt).ToBeHiddenAsync();

            await sut.SelectSuppliersAsync(selectedSupplierNames);
            await sut.SubmitSelectionAsync();

            await page.WaitForURLAsync(BOOKING_SUMMARY_URL_PATTERN);
            page.Url.ShouldContain(BOOKING_SUMMARY_PATH);
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
    public Task EnterBooking_RemainsUsableAndRecoverable_WhenBookingLookupApiFails() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            var booking = Fixture.SeedData.BookingSelectionBooking;
            var selectedSupplierNames = Fixture.SeedData.ActiveSuppliers
                .Take(Fixture.SeedData.RequiredSupplierSelectionCount)
                .Select(supplier => supplier.Name)
                .ToList();
            bool failFirstLookup = true;

            await page.RouteAsync("**/api/foodtour/checkbooking", async route => {
                if (failFirstLookup) {
                    failFirstLookup = false;
                    await route.FulfillAsync(new RouteFulfillOptions {
                        Status = 500,
                        ContentType = "application/json",
                        Body = "{}"
                    });
                    return;
                }

                await route.ContinueAsync();
            });

            await sut.GotoAsync();
            await sut.EnterBookingLookupDetailsAsync(booking.CustomerEmail, booking.PurchaseCode);
            await page.WaitForTimeoutAsync(700);

            await Expect(sut.BookingStatusMessage).ToBeHiddenAsync();
            await Expect(sut.PartnerConfirmationPrompt).ToBeHiddenAsync();

            await sut.PurchaseCodeInput.FillAsync(string.Empty);
            await sut.PurchaseCodeInput.FillAsync(booking.PurchaseCode);
            await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_FOUND_MESSAGE);

            await sut.SelectSuppliersAsync(selectedSupplierNames);
            await sut.SubmitSelectionAsync();

            await page.WaitForURLAsync(BOOKING_SUMMARY_URL_PATTERN);
            page.Url.ShouldContain(BOOKING_SUMMARY_PATH);
        });
    }

    [Fact]
    public Task EnterBooking_DoesNotOverwriteNewerInput_WhenEarlierLookupResponseArrivesLate() {
        return ExecutePageTestAsync(async page => {
            var sut = new BookingSelectionPage(page);
            const string FIRST_LOOKUP_EMAIL = "stale.response@example.com";
            const string FIRST_LOOKUP_PURCHASE_CODE = "FIRST1234";
            const string LATEST_LOOKUP_EMAIL = "latest.input@example.com";
            const string LATEST_LOOKUP_PURCHASE_CODE = "SECOND123";

            await page.RouteAsync("**/api/foodtour/checkbooking", async route => {
                string requestBody = route.Request.PostData ?? string.Empty;

                if (requestBody.Contains(FIRST_LOOKUP_EMAIL, StringComparison.Ordinal) &&
                    requestBody.Contains(FIRST_LOOKUP_PURCHASE_CODE, StringComparison.Ordinal)) {
                    await Task.Delay(1200, TestContext.Current.CancellationToken);
                    await route.FulfillAsync(new RouteFulfillOptions {
                        Status = 200,
                        ContentType = "application/json",
                        Body = """{"found":true,"message":"We've found your booking!","bookingId":123,"tourDate":"2035-06-11T11:15:00Z","isCancelled":false,"isConfirmed":false,"requiresPartnerConfirmation":false}"""
                    });
                    return;
                }

                if (requestBody.Contains(LATEST_LOOKUP_EMAIL, StringComparison.Ordinal) &&
                    requestBody.Contains(LATEST_LOOKUP_PURCHASE_CODE, StringComparison.Ordinal)) {
                    await route.FulfillAsync(new RouteFulfillOptions {
                        Status = 200,
                        ContentType = "application/json",
                        Body = """{"found":false,"message":"We couldn't find your booking.","requiresPartnerConfirmation":false}"""
                    });
                    return;
                }

                await route.ContinueAsync();
            });

            await sut.GotoAsync();
            await sut.EnterBookingLookupDetailsAsync(FIRST_LOOKUP_EMAIL, FIRST_LOOKUP_PURCHASE_CODE);
            await page.WaitForTimeoutAsync(700);

            await sut.EnterBookingLookupDetailsAsync(LATEST_LOOKUP_EMAIL, LATEST_LOOKUP_PURCHASE_CODE);
            await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_NOT_FOUND_MESSAGE);

            await page.WaitForTimeoutAsync(900);

            await Expect(sut.BookingStatusMessage).ToContainTextAsync(BOOKING_NOT_FOUND_MESSAGE);
            await Expect(sut.PartnerConfirmationPrompt).ToBeHiddenAsync();
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
