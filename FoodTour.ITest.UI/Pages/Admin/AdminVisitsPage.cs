namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminVisitsPage(IPage page) {
    private const string VISITS_HEADING = "Visits";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = VISITS_HEADING,
        Level = 1
    });

    public ILocator VisitsTable => page.Locator("table");

    public async Task GotoAsync() {
        await page.GotoAsync("/Admin/Visits");
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task ToggleReimbursementAsync(int visitId) {
        var form = page.Locator("form.js-mark-reimbursed").Filter(new LocatorFilterOptions {
            Has = page.Locator($"input[name=\"id\"][value=\"{visitId}\"]")
        });
        await form.GetByRole(AriaRole.Button).ClickAsync();
    }

    public async Task<bool> IsVisitReimbursedAsync(int visitId) {
        var row = page.Locator($"tr:has-text('{visitId}')");
        var reimbursedCell = row.Locator(".visit-reimbursed");
        int count = await reimbursedCell.GetByText("Reimbursed").CountAsync();
        return count > 0;
    }
}
