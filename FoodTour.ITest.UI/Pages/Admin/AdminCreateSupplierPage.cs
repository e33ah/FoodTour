namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminCreateSupplierPage(IPage page) {
    private const string CREATE_SUPPLIER_HEADING = "Create Supplier";
    private const string SUPPLIER_NAME_LABEL = "Business Name";
    private const string ADDRESS_LABEL = "Business Address";
    private const string CONTACT_NAME_LABEL = "Contact name";
    private const string CONTACT_EMAIL_LABEL = "Contact email";
    private const string CONFIRMATION_CODE_LABEL = "Confirmation code";
    private const string OPENING_TIMES_LABEL = "Opening times (general)";
    private const string IS_ACTIVE_LABEL = "Active";
    private const string SAVE_BUTTON = "Create";
    private const string CANCEL_LINK = "Cancel";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = CREATE_SUPPLIER_HEADING
    });

    public ILocator NameInput => page.GetByLabel(SUPPLIER_NAME_LABEL);
    public ILocator AddressInput => page.GetByLabel(ADDRESS_LABEL);
    public ILocator ContactNameInput => page.GetByLabel(CONTACT_NAME_LABEL);
    public ILocator ContactEmailInput => page.GetByLabel(CONTACT_EMAIL_LABEL);
    public ILocator ConfirmationCodeInput => page.GetByLabel(CONFIRMATION_CODE_LABEL);
    public ILocator OpeningTimesInput => page.GetByLabel(OPENING_TIMES_LABEL);
    public ILocator IsActiveCheckbox => page.GetByLabel(IS_ACTIVE_LABEL);

    public ILocator SaveButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = SAVE_BUTTON
    });

    public ILocator CancelLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = CANCEL_LINK
    });

    public async Task GotoAsync() {
        await page.GotoAsync("/Admin/CreateSupplier");
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task FillFormAsync(string name, string address, string contactName, string contactEmail, string confirmationCode, string openingTimes, bool isActive = true) {
        await NameInput.FillAsync(name);
        await AddressInput.FillAsync(address);
        await ContactNameInput.FillAsync(contactName);
        await ContactEmailInput.FillAsync(contactEmail);
        await ConfirmationCodeInput.FillAsync(confirmationCode);
        await OpeningTimesInput.FillAsync(openingTimes);

        if (isActive) {
            await IsActiveCheckbox.CheckAsync();
        } else {
            await IsActiveCheckbox.UncheckAsync();
        }
    }

    public async Task ClickSaveAsync() {
        await SaveButton.ClickAsync();
    }
}
