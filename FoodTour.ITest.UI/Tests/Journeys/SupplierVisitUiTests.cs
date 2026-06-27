namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class SupplierVisitUiTests(FoodTourApplicationFixture fixture) : UiTestBase(fixture) {
    private const string SUPPLIER_VISIT_PATH_TEMPLATE = "/Home/SupplierVisit?qrToken={0}";
    private const string TOUR_CANCELLED_URL_PATTERN = "**/Home/TourCancelled*";
    private const string TOUR_COMPLETED_URL_PATTERN = "**/Home/TourCompleted*";
    private const string VISIT_CONFIRMATION_PATH = "/Home/VisitConfirmation";
    private const string VISIT_CONFIRMATION_URL_PATTERN = "**/Home/VisitConfirmation?*";

    [Fact]
    public Task ConfirmVisit_NavigatesToVisitConfirmation_WhenSupplierCodeIsValid() {
        return ExecutePageTestAsync(async page => {
            var sut = new SupplierVisitPage(page);
            var booking = Fixture.SeedData.SupplierVisitBooking;
            var firstVisit = Fixture.SeedData.SupplierVisitBookingVisits[0];
            var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == firstVisit.SupplierId);

            await sut.GotoAsync(booking.QrToken);
            await sut.ConfirmVisitAsync(supplier.Name, supplier.ConfirmationCode);

            await page.WaitForURLAsync(VISIT_CONFIRMATION_URL_PATTERN);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            page.Url.ShouldContain(VISIT_CONFIRMATION_PATH);
            var visitConfirmationPage = new VisitConfirmationPage(page);

            await Expect(visitConfirmationPage.SuccessHeading).ToBeVisibleAsync();
            await Expect(visitConfirmationPage.PreparationAlert).ToContainTextAsync(booking.TotalGuests.ToString());
        });
    }

    [Fact]
    public Task ConfirmVisit_ShowsAccessibleError_WhenSupplierCodeIsInvalid() {
        return ExecutePageTestAsync(async page => {
            var sut = new SupplierVisitPage(page);
            var booking = Fixture.SeedData.SupplierVisitBooking;
            var firstVisit = Fixture.SeedData.SupplierVisitBookingVisits[0];
            var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == firstVisit.SupplierId);

            await sut.GotoAsync(booking.QrToken);
            await sut.ConfirmVisitWithInvalidCodeAsync(supplier.Name);

            await Expect(sut.SupplierErrorRegion).ToBeVisibleAsync();
            page.Url.ShouldContain("/Home/SupplierVisit");
        });
    }

    [Fact]
    public Task OpenSupplierVisit_RedirectsToTourCancelled_WhenBookingHasBeenCancelled() {
        return ExecutePageTestAsync(async page => {
            var booking = Fixture.SeedData.CancelledBooking;

            await page.GotoAsync(string.Format(SUPPLIER_VISIT_PATH_TEMPLATE, booking.QrToken));

            await page.WaitForURLAsync(TOUR_CANCELLED_URL_PATTERN);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            var tourCancelledPage = new TourCancelledPage(page);

            await Expect(tourCancelledPage.PageHeading).ToBeVisibleAsync();
        });
    }

    [Fact]
    public Task OpenSupplierVisit_ShowsEarlyState_WhenTourDateIsInTheFuture() {
        return ExecutePageTestAsync(async page => {
            var sut = new SupplierVisitPage(page);
            var booking = Fixture.SeedData.ConfirmedBooking;

            await page.GotoAsync(string.Format(SUPPLIER_VISIT_PATH_TEMPLATE, booking.QrToken));

            await Expect(sut.EarlyHeading).ToBeVisibleAsync();
        });
    }

    [Fact]
    public Task OpenSupplierVisit_ShowsEndedState_WhenTourDateIsInThePast() {
        return ExecutePageTestAsync(async page => {
            var sut = new SupplierVisitPage(page);
            var booking = Fixture.SeedData.PastBooking;

            await page.GotoAsync(string.Format(SUPPLIER_VISIT_PATH_TEMPLATE, booking.QrToken));

            await Expect(sut.EndedHeading).ToBeVisibleAsync();
        });
    }

    [Fact]
    public Task OpenSupplierVisit_RedirectsToTourCompleted_WhenAllVisitsAreClaimed() {
        return ExecutePageTestAsync(async page => {
            var booking = Fixture.SeedData.SupplierVisitBooking;

            try {
                await SetAllSupplierVisitClaimsAsync(true);

                await page.GotoAsync(string.Format(SUPPLIER_VISIT_PATH_TEMPLATE, booking.QrToken));

                await page.WaitForURLAsync(TOUR_COMPLETED_URL_PATTERN);
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                var tourCompletedPage = new TourCompletedPage(page);

                await Expect(tourCompletedPage.PageHeading).ToBeVisibleAsync();
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
