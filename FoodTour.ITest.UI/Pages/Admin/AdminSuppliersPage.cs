namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminSuppliersPage(IPage page) {
    private const string SUPPLIERS_HEADING = "Suppliers";
    private const string ADD_SUPPLIER_BUTTON = "Add Supplier";
    private const string EDIT_BUTTON = "Edit";
    private const string DISABLE_BUTTON = "Disable";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = SUPPLIERS_HEADING,
        Level = 1
    });

    public ILocator SuppliersTable => page.Locator("table");

    public ILocator AddSupplierButton => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = ADD_SUPPLIER_BUTTON
    });

    public async Task GotoAsync() {
        await page.GotoAsync("/Admin/Suppliers");
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task ClickAddSupplierAsync() {
        await AddSupplierButton.ClickAsync();
    }

    public async Task ClickEditSupplierAsync(string supplierName) {
        var row = page.Locator($"tr:has-text('{supplierName}')");
        await row.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions {
            Name = EDIT_BUTTON
        }).ClickAsync();
    }

    public async Task ClickDisableSupplierAsync(string supplierName) {
        var row = page.Locator($"tr:has-text('{supplierName}')");
        await row.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions {
            Name = DISABLE_BUTTON
        }).ClickAsync();
    }

    public async Task<bool> HasSupplierAsync(string supplierName) {
        int count = await page.GetByText(supplierName).CountAsync();
        return count > 0;
    }

    public async Task<bool> IsSupplierActiveAsync(string supplierName) {
        var row = page.Locator($"tr:has-text('{supplierName}')");
        int count = await row.GetByText("Active").CountAsync();
        return count > 0;
    }
}
