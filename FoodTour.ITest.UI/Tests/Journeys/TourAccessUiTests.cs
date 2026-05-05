namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class TourAccessUiTests(FoodTourApplicationFixture fixture) : UiTestBase(fixture) {
    private const string FOOD_TOUR_CODE_PATH_TEMPLATE = "/Home/FoodTourCode?qrToken={0}";
    private const string FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE = "**/Home/FoodTourCode?qrToken={0}";
    private const string TOUR_CANCELLED_URL_PATTERN = "**/Home/TourCancelled?*";

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
    public Task OpenFoodTourCode_ShowsCompletedState_WhenAllVisitsAreClaimed() {
        return ExecutePageTestAsync(async page => {
            var booking = Fixture.SeedData.SupplierVisitBooking;

            try {
                await SetAllSupplierVisitClaimsAsync(true);

                await page.GotoAsync(string.Format(FOOD_TOUR_CODE_PATH_TEMPLATE, booking.QrToken));

                await page.WaitForURLAsync(string.Format(FOOD_TOUR_CODE_URL_PATTERN_TEMPLATE, booking.QrToken));
                page.Url.ShouldContain("/Home/FoodTourCode");
                var foodTourQrCodePage = new FoodTourQRCodePage(page);

                await Expect(foodTourQrCodePage.TourCompleteHeading).ToBeVisibleAsync();
            } finally {
                await SetAllSupplierVisitClaimsAsync(false);
            }
        });
    }

    private async Task SetAllSupplierVisitClaimsAsync(bool isClaimed) {
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(Fixture.ConnectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);
        var visits = await context.Visits
            .Where(visit => visit.BookingId == Fixture.SeedData.SupplierVisitBooking.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        foreach (var visit in visits) {
            visit.IsClaimed = isClaimed;
            visit.ClaimedAt = isClaimed ? DateTime.UtcNow : null;
            visit.TastingExperiencesClaimed = isClaimed ? Fixture.SeedData.SupplierVisitBooking.TotalGuests : 0;
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
