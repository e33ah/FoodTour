namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public partial class PartnerBookingConfirmationUiTests(FoodTourApplicationFixture fixture) {
    private const string COMPLETE_PARTNER_BOOKING_CONFIRMATION_PATH = "/Home/CompletePartnerBookingConfirmation";
    private const string EXPIRED_LINK_MESSAGE = "This link has expired, please request a new one.";
    private const string INVALID_LINK_MESSAGE = "This confirmation link is invalid. Please request a new one.";
    private const string PARTNER_BOOKING_CONFIRMATION_PATH = "/Home/PartnerBookingConfirmation";
    private const string PARTNER_BOOKING_CONFIRMATION_URL_PATTERN = "**/Home/PartnerBookingConfirmation?*";
    private const string PARTNER_CONFIRMATION_EMAIL = "partner.confirmation@example.com";

    [Fact]
    public async Task ConfirmPartnerBooking_SendsEmailAndCompletesValidation_WhenBookingRequiresConfirmation() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var bookingSelectionPage = new BookingSelectionPage(page);
        var booking = fixture.SeedData.PartnerConfirmationBooking;
        fixture.TestEmailSender.Clear();

        try {
            await bookingSelectionPage.GotoAsync();
            await bookingSelectionPage.EnterBookingLookupDetailsAsync(PARTNER_CONFIRMATION_EMAIL, booking.VoucherNumber!);

            await Expect(bookingSelectionPage.PartnerConfirmationLink).ToBeVisibleAsync();
            string partnerConfirmationUrl = await bookingSelectionPage.PartnerConfirmationLink.GetAttributeAsync("href")
                ?? throw new InvalidOperationException("The partner confirmation prompt did not generate a link.");
            partnerConfirmationUrl.ShouldContain(PARTNER_BOOKING_CONFIRMATION_PATH);

            await page.GotoAsync(partnerConfirmationUrl);
            await page.WaitForURLAsync(PARTNER_BOOKING_CONFIRMATION_URL_PATTERN);
            page.Url.ShouldContain(PARTNER_BOOKING_CONFIRMATION_PATH);
            var partnerBookingConfirmationPage = new PartnerBookingConfirmationPage(page);

            await Expect(partnerBookingConfirmationPage.PageHeading).ToBeVisibleAsync();
            await Expect(partnerBookingConfirmationPage.CustomerEmailInput).ToHaveValueAsync(PARTNER_CONFIRMATION_EMAIL);
            await Expect(partnerBookingConfirmationPage.BookingReferenceInput).ToHaveValueAsync(booking.VoucherNumber!);

            await partnerBookingConfirmationPage.SendConfirmationEmailButton.ClickAsync();

            await Expect(partnerBookingConfirmationPage.StatusAlert).ToContainTextAsync(PARTNER_CONFIRMATION_EMAIL);
            fixture.TestEmailSender.SentEmails.Count.ShouldBe(1);
            fixture.TestEmailSender.SentEmails.Single().To.ShouldBe(PARTNER_CONFIRMATION_EMAIL);

            string validationUrl = ExtractValidationUrl(fixture.TestEmailSender.SentEmails.Single().HtmlBody);
            validationUrl.ShouldContain(COMPLETE_PARTNER_BOOKING_CONFIRMATION_PATH);

            await page.GotoAsync(validationUrl);
            await Expect(bookingSelectionPage.PageHeading).ToBeVisibleAsync();
            await Expect(bookingSelectionPage.CustomerEmailInput).ToHaveValueAsync(PARTNER_CONFIRMATION_EMAIL);
            await Expect(bookingSelectionPage.PurchaseCodeInput).ToHaveValueAsync(booking.VoucherNumber!);
        } finally {
            DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
            optionsBuilder.UseSqlServer(fixture.ConnectionString);

            await using var context = new FoodTourContext(optionsBuilder.Options);
            var partnerBooking = await context.Bookings.SingleAsync(
                existingBooking => existingBooking.Id == booking.Id,
                TestContext.Current.CancellationToken);
            partnerBooking.RequiresEmailConfirmation = true;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task CompletePartnerBookingConfirmation_ShowsInvalidWarning_WhenValidationTokenIsInvalid() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();

        await page.GotoAsync($"{COMPLETE_PARTNER_BOOKING_CONFIRMATION_PATH}?validation=invalid-token");
        await page.WaitForURLAsync(PARTNER_BOOKING_CONFIRMATION_URL_PATTERN);
        var sut = new PartnerBookingConfirmationPage(page);

        await Expect(sut.PageHeading).ToBeVisibleAsync();
        await Expect(sut.StatusAlert).ToContainTextAsync(INVALID_LINK_MESSAGE);
    }

    [Fact]
    public async Task CompletePartnerBookingConfirmation_ShowsExpiredWarning_WhenValidationTokenHasExpired() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var booking = fixture.SeedData.PartnerConfirmationBooking;
        using var serviceScope = fixture.Services.CreateScope();
        var partnerBookingConfirmationService = serviceScope.ServiceProvider.GetRequiredService<PartnerBookingConfirmationService>();
        string expiredValidation = partnerBookingConfirmationService.CreateValidationToken(
            booking.Id,
            PARTNER_CONFIRMATION_EMAIL,
            booking.VoucherNumber!,
            DateTime.UtcNow.AddMinutes(-1));

        await page.GotoAsync($"{COMPLETE_PARTNER_BOOKING_CONFIRMATION_PATH}?validation={Uri.EscapeDataString(expiredValidation)}");
        await page.WaitForURLAsync(PARTNER_BOOKING_CONFIRMATION_URL_PATTERN);
        var sut = new PartnerBookingConfirmationPage(page);

        await Expect(sut.PageHeading).ToBeVisibleAsync();
        await Expect(sut.StatusAlert).ToContainTextAsync(EXPIRED_LINK_MESSAGE);
    }

    private static string ExtractValidationUrl(string emailHtmlBody) {
        var result = CompletePartnerBookingConfirmationUrlRegex().Match(emailHtmlBody);

        result.Success.ShouldBeTrue();
        return result.Value.Replace("&amp;", "&", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"https?://[^""'\s>]*CompletePartnerBookingConfirmation[^""'\s>]*")]
    private static partial Regex CompletePartnerBookingConfirmationUrlRegex();
}
