namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public partial class PartnerBookingConfirmationUiTests(FoodTourApplicationFixture fixture) {
    private const string COMPLETE_PARTNER_BOOKING_CONFIRMATION_PATH = "/Home/CompletePartnerBookingConfirmation";
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

            await Expect(partnerBookingConfirmationPage.EmailSentAlert).ToContainTextAsync(PARTNER_CONFIRMATION_EMAIL);
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

    private static string ExtractValidationUrl(string emailHtmlBody) {
        var result = CompletePartnerBookingConfirmationUrlRegex().Match(emailHtmlBody);

        result.Success.ShouldBeTrue();
        return result.Value.Replace("&amp;", "&", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"https?://[^""'\s>]*CompletePartnerBookingConfirmation[^""'\s>]*")]
    private static partial Regex CompletePartnerBookingConfirmationUrlRegex();
}
