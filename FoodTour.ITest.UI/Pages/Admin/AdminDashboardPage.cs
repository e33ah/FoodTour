namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminDashboardPage(IPage page) {
    private const string DASHBOARD_HEADING = "Dashboard";
    private const string BOOKINGS_LINK = "Bookings";
    private const string VISITS_LINK = "Visits";
    private const string SUPPLIERS_LINK = "Suppliers";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = DASHBOARD_HEADING
    });

    public ILocator BookingsLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = BOOKINGS_LINK
    });

    public ILocator VisitsLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = VISITS_LINK
    });

    public ILocator SuppliersLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = SUPPLIERS_LINK
    });

    public async Task GotoAsync() {
        await page.GotoAsync("/Admin/Dashboard");
        await Expect(PageHeading).ToBeVisibleAsync();
    }
}
