namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminBookingsPage(IPage page) {
    private const string BOOKINGS_HEADING = "Bookings";
    private const string FILTER_ALL = "All";
    private const string FILTER_UPCOMING = "Upcoming";
    private const string FILTER_PAST = "Past";
    private const string FILTER_CANCELLED = "Cancelled";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKINGS_HEADING,
        Level = 1
    });

    public ILocator BookingsTable => page.Locator("table");

    public ILocator FilterAllLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = FILTER_ALL
    });

    public ILocator FilterUpcomingLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = FILTER_UPCOMING
    });

    public ILocator FilterPastLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = FILTER_PAST
    });

    public ILocator FilterCancelledLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = FILTER_CANCELLED
    });

    public async Task GotoAsync() {
        await page.GotoAsync("/Admin/Bookings");
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task ClickBookingDetailAsync(string purchaseCode) {
        var row = page.Locator("tr").Filter(new LocatorFilterOptions {
            HasText = purchaseCode
        });
        await row.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions {
            Name = "View"
        }).ClickAsync();
    }

    public async Task<bool> HasBookingAsync(string customerEmail) {
        int count = await page.GetByText(customerEmail).CountAsync();
        return count > 0;
    }
}
