namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public sealed class AdminBookingsUiTests(FoodTourApplicationFixture fixture) : UiTestBase(fixture) {
    [Fact]
    public Task ViewBookingsList_ShowsBookings_WhenAuthenticated() {
        return ExecutePageTestAsync(async page => {
            var sut = new AdminBookingsPage(page);

            await sut.GotoAsync();

            await Expect(sut.PageHeading).ToBeVisibleAsync();
            await Expect(sut.BookingsTable).ToBeVisibleAsync();
        });
    }

    [Fact]
    public Task ViewBookingsList_ShowsCorrectBooking_WhenFiltered() {
        return ExecutePageTestAsync(async page => {
            var sut = new AdminBookingsPage(page);
            var cancelledBooking = Fixture.SeedData.CancelledBooking;

            await sut.GotoAsync();
            await sut.FilterCancelledLink.ClickAsync();

            bool hasBooking = await sut.HasBookingAsync(cancelledBooking.CustomerEmail);
            hasBooking.ShouldBeTrue();
        });
    }

    [Fact]
    public Task ViewBookingDetail_ShowsBookingInformation_WhenBookingExists() {
        return ExecutePageTestAsync(async page => {
            var sut = new AdminBookingsPage(page);
            var confirmedBooking = Fixture.SeedData.ConfirmedBooking;

            await sut.GotoAsync();
            await sut.ClickBookingDetailAsync(confirmedBooking.PurchaseCode);

            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            page.Url.ShouldEndWith($"/Admin/BookingDetail/{confirmedBooking.Id}");
            string html = await page.ContentAsync();
            html.ShouldContain(confirmedBooking.CustomerEmail);
        });
    }

    [Fact]
    public Task NavigateToBookings_FromDashboard_ShowsBookingsPage() {
        return ExecutePageTestAsync(async page => {
            var dashboardPage = new AdminDashboardPage(page);
            var bookingsPage = new AdminBookingsPage(page);

            await dashboardPage.GotoAsync();
            await dashboardPage.BookingsTileLink.ClickAsync();

            await Expect(bookingsPage.PageHeading).ToBeVisibleAsync();
        });
    }
}
