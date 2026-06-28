namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminSuppliersPage(IPage page) {
    private const string SUPPLIERS_HEADING = "Suppliers";
    private const string ADD_SUPPLIER_BUTTON = "Add Supplier";
    private const string EDIT_BUTTON = "Edit";
    private const string DISABLE_BUTTON = "Disable";
    private const string ALL_FILTER = "All";
    private const string ACTIVE_FILTER = "Active";
    private const string INACTIVE_FILTER = "Inactive";
    private const string NAME_HEADER = "Name";
    private const string CONTACT_HEADER = "Contact";
    private const string ACTIVE_HEADER = "Active , click to sort";
    private const string EMAILS_HEADER = "Emails";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = SUPPLIERS_HEADING,
        Level = 1
    });

    public ILocator SuppliersTable => page.Locator("table");

    public ILocator AddSupplierButton => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = ADD_SUPPLIER_BUTTON
    });

    public ILocator AllFilter => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = ALL_FILTER });
    public ILocator ActiveFilter => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = ACTIVE_FILTER, Exact = true });
    public ILocator InactiveFilter => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = INACTIVE_FILTER });
    public ILocator NameHeader => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = NAME_HEADER });
    public ILocator ContactHeader => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = CONTACT_HEADER });
    public ILocator ActiveHeader => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = ACTIVE_HEADER, });
    public ILocator EmailsHeader => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = EMAILS_HEADER });

    public async Task GotoAsync() {
        await page.GotoAsync("/Admin/Suppliers");
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task ClickAddSupplierAsync() {
        await AddSupplierButton.ClickAsync();
    }

    public async Task ClickAllFilterAsync() => await AllFilter.ClickAsync();

    public async Task ClickActiveFilterAsync() => await ActiveFilter.ClickAsync();

    public async Task ClickInactiveFilterAsync() => await InactiveFilter.ClickAsync();

    public async Task ClickNameHeaderAsync() => await NameHeader.ClickAsync();

    public async Task ClickContactHeaderAsync() => await ContactHeader.ClickAsync();

    public async Task ClickActiveHeaderAsync() => await ActiveHeader.ClickAsync();

    public async Task ClickEmailsHeaderAsync() => await EmailsHeader.ClickAsync();

    public async Task ClickEditSupplierAsync(string supplierName) {
        var row = page.Locator("tr").Filter(new LocatorFilterOptions {
            HasText = supplierName
        });
        await row.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions {
            Name = EDIT_BUTTON
        }).ClickAsync();
    }

    public async Task ClickDisableSupplierAsync(string supplierName) {
        var row = page.Locator("tr").Filter(new LocatorFilterOptions {
            HasText = supplierName
        });
        await row.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions {
            Name = DISABLE_BUTTON
        }).ClickAsync();
    }

    public async Task<bool> HasSupplierAsync(string supplierName) {
        int count = await page.GetByText(supplierName).CountAsync();
        return count > 0;
    }

    public async Task<string?> GetSupplierRowClassAsync(string supplierName) {
        var row = page.Locator("tr").Filter(new LocatorFilterOptions {
            HasText = supplierName
        });
        return await row.GetAttributeAsync("class");
    }

    public async Task<bool> IsSupplierActiveAsync(string supplierName) {
        var row = page.Locator("tr").Filter(new LocatorFilterOptions {
            HasText = supplierName
        });
        int count = await row.GetByText("Active").CountAsync();
        return count > 0;
    }

    public async Task<IReadOnlyList<string>> GetSupplierNamesAsync() {
        string[] names = await page.Locator("tbody tr").EvaluateAllAsync<string[]>("rows => rows.map(row => row.querySelector('td .fw-semibold')?.textContent?.trim() ?? '')");
        return names;
    }
}
