namespace FoodTour.ITest.UI.Tests.Journeys;

[Collection("FoodTour application")]
public sealed class AdminVisitsUiTests(FoodTourApplicationFixture fixture) : UiTestBase(fixture) {
    [Fact]
    public Task ViewVisitsList_ShowsVisits_WhenAuthenticated() {
        return ExecutePageTestAsync(async page => {
            var sut = new AdminVisitsPage(page);

            await sut.GotoAsync();

            await Expect(sut.PageHeading).ToBeVisibleAsync();
            await Expect(sut.VisitsTable).ToBeVisibleAsync();
        });
    }

    [Fact]
    public Task MarkVisitReimbursed_UpdatesVisitStatus_WhenToggled() {
        return ExecutePageTestAsync(async page => {
            var sut = new AdminVisitsPage(page);
            int visitId = Fixture.SeedData.SupplierVisitBookingVisits[0].Id;

            await EnsureVisitClaimedAsync(visitId);

            await sut.GotoAsync();
            bool isReimbursedBefore = await sut.IsVisitReimbursedAsync(visitId);

            await sut.ToggleReimbursementAsync(visitId);
            await page.WaitForTimeoutAsync(500);

            bool isReimbursedAfter = await sut.IsVisitReimbursedAsync(visitId);
            isReimbursedAfter.ShouldNotBe(isReimbursedBefore);
        });
    }

    [Fact]
    public Task NavigateToVisits_FromDashboard_ShowsVisitsPage() {
        return ExecutePageTestAsync(async page => {
            var dashboardPage = new AdminDashboardPage(page);
            var visitsPage = new AdminVisitsPage(page);

            await dashboardPage.GotoAsync();
            await dashboardPage.VisitsTileLink.ClickAsync();

            await Expect(visitsPage.PageHeading).ToBeVisibleAsync();
            page.Url.ShouldContain("/Admin/Visits?filter=unreimbursed");
        });
    }

    private async Task EnsureVisitClaimedAsync(int visitId) {
        await using var context = Fixture.CreateDbContext();
        var visit = await context.Visits.FindAsync([visitId], TestContext.Current.CancellationToken);
        visit.ShouldNotBeNull();
        if (visit.IsClaimed) {
            return;
        }

        visit.IsClaimed = true;
        visit.ClaimedAt = DateTime.UtcNow;
        visit.TastingExperiencesClaimed = Fixture.SeedData.SupplierVisitBooking.TotalGuests;
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
