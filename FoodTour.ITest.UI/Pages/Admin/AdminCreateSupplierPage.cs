namespace FoodTour.ITest.UI.Pages.Admin;

public sealed class AdminCreateSupplierPage(IPage page) {
    private const string CREATE_SUPPLIER_HEADING = "Create Supplier";
    private const string SUPPLIER_NAME_LABEL = "Business Name";
    private const string ADDRESS_LABEL = "Business Address";
    private const string CONTACT_NAME_LABEL = "Contact name";
    private const string CONTACT_EMAIL_LABEL = "Contact email";
    private const string CONFIRMATION_CODE_LABEL = "Confirmation code";
    private const string OPENING_TIMES_LABEL = "Opening times (default)";
    private const string IS_ACTIVE_LABEL = "Active";
    private const string SAVE_BUTTON = "Save";
    private const string CANCEL_LINK = "Cancel";
    private const string EMAIL_NOTIFICATIONS_LABEL = "Email notifications";
    private const string CONFIRMATION_CODE_CHECKBOX = "Require confirmation code";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = CREATE_SUPPLIER_HEADING });

    public ILocator NameInput => page.GetByLabel(SUPPLIER_NAME_LABEL);
    public ILocator AddressInput => page.GetByLabel(ADDRESS_LABEL);
    public ILocator ContactNameInput => page.GetByLabel(CONTACT_NAME_LABEL);
    public ILocator ContactEmailInput => page.GetByLabel(CONTACT_EMAIL_LABEL);
    public ILocator ConfirmationCodeInput => page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = CONFIRMATION_CODE_LABEL });
    public ILocator OpeningTimesInput => page.GetByLabel(OPENING_TIMES_LABEL);
    public ILocator IsActiveCheckbox => page.GetByLabel(IS_ACTIVE_LABEL);
    public ILocator RequiresConfirmationCodeCheckbox => page.GetByLabel(CONFIRMATION_CODE_CHECKBOX);
    public ILocator EmailNotificationsCheckbox => page.GetByLabel(EMAIL_NOTIFICATIONS_LABEL);

    public ILocator SaveButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = SAVE_BUTTON }).First;
    public ILocator CancelLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = CANCEL_LINK });

    public async Task GotoAsync() {
        await page.GotoAsync("/Admin/CreateSupplier");
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task FillFormAsync(string name, string address, string contactName, string contactEmail, string confirmationCode, string openingTimes, bool isActive = true, bool requiresConfirmationCode = true, bool emailNotifications = true) {
        await SetCheckbox(isActive, IsActiveCheckbox);
        await SetCheckbox(requiresConfirmationCode, RequiresConfirmationCodeCheckbox);
        await SetCheckbox(emailNotifications, EmailNotificationsCheckbox);
        await NameInput.FillAsync(name);
        await AddressInput.FillAsync(address);
        await ContactNameInput.FillAsync(contactName);
        await ContactEmailInput.FillAsync(contactEmail);
        await ConfirmationCodeInput.FillAsync(confirmationCode);
        await OpeningTimesInput.FillAsync(openingTimes);
    }

    private static async Task SetCheckbox(bool check, ILocator checkbox) {
        if (check) {
            await checkbox.CheckAsync();
        } else {
            await checkbox.UncheckAsync();
        }
    }

    public async Task ClickSaveAsync() => await SaveButton.ClickAsync();
}
