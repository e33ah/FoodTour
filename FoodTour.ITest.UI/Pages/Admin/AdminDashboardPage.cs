namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminDashboardPage(IPage page) {
    private const string DASHBOARD_HEADING = "Dashboard";
    private const string BOOKINGS_TILE_LINK = "Open bookings tab";
    private const string VISITS_TILE_LINK = "Open visits awaiting reimbursement tab";
    private const string SUPPLIERS_TILE_LINK = "Open active suppliers tab";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = DASHBOARD_HEADING
    });

    public ILocator BookingsTileLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = BOOKINGS_TILE_LINK
    });

    public ILocator VisitsTileLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = VISITS_TILE_LINK
    });

    public ILocator SuppliersTileLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = SUPPLIERS_TILE_LINK
    });

    public async Task GotoAsync() {
        await page.GotoAsync("/Admin/Dashboard");
        await Expect(PageHeading).ToBeVisibleAsync();
    }
}
