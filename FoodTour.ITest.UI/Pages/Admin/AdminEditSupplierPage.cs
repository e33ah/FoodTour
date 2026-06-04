namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminEditSupplierPage(IPage page) {
    private const string EDIT_SUPPLIER_HEADING = "Edit Supplier";
    private const string SUPPLIER_NAME_LABEL = "Business Name";
    private const string ADDRESS_LABEL = "Business Address";
    private const string CONTACT_NAME_LABEL = "Contact name";
    private const string CONTACT_EMAIL_LABEL = "Contact email";
    private const string OPENING_TIMES_LABEL = "Opening times (general)";
    private const string IS_ACTIVE_LABEL = "Active";
    private const string SAVE_BUTTON = "Save";
    private const string CANCEL_LINK = "Cancel";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = EDIT_SUPPLIER_HEADING
    });

    public ILocator NameInput => page.GetByLabel(SUPPLIER_NAME_LABEL);
    public ILocator AddressInput => page.GetByLabel(ADDRESS_LABEL);
    public ILocator ContactNameInput => page.GetByLabel(CONTACT_NAME_LABEL);
    public ILocator ContactEmailInput => page.GetByLabel(CONTACT_EMAIL_LABEL);
    public ILocator OpeningTimesInput => page.GetByLabel(OPENING_TIMES_LABEL);
    public ILocator IsActiveCheckBox => page.GetByLabel(IS_ACTIVE_LABEL);

    public ILocator SaveButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = SAVE_BUTTON
    });

    public ILocator CancelLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = CANCEL_LINK
    });

    public async Task GotoAsync(int supplierId) {
        await page.GotoAsync($"/Admin/EditSupplier/{supplierId}");
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task UpdateOpeningTimesAsync(string openingTimes) {
        await OpeningTimesInput.ClearAsync();
        await OpeningTimesInput.FillAsync(openingTimes);
    }

    public async Task ClickSaveAsync() {
        await SaveButton.ClickAsync();
    }

    public async Task<string?> GetNameValueAsync() {
        return await NameInput.InputValueAsync();
    }

    public async Task UncheckIsActiveAsync() {
        await IsActiveCheckBox.UncheckAsync();
    }
}
