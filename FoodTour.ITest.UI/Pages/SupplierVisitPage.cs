namespace FoodTour.ITest.UI.Pages;

public sealed class SupplierVisitPage(IPage page) {
    private const string CONFIRM_VISIT_BUTTON = "Confirm Visit";
    private const string FOOD_TOUR_VISIT_HEADING = "Food Tour Visit";
    private const string INVALID_BUSINESS_CODE_MESSAGE = "Invalid business code provided. Please try again or contact Support.";
    private const string SELECT_BUSINESS_LABEL = "Please select your business:";
    private const string SUPPLIER_CODE_LABEL = "Enter your business Code:";
    private const string SUPPLIER_VISIT_PATH_TEMPLATE = "/Home/SupplierVisit?qrToken={0}";

    public ILocator ConfirmVisitButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = CONFIRM_VISIT_BUTTON
    });

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = FOOD_TOUR_VISIT_HEADING
    });

    public ILocator SupplierCodeInput => page.GetByLabel(SUPPLIER_CODE_LABEL);

    public ILocator SupplierErrorRegion => page.GetByRole(AriaRole.Alert);

    public ILocator SupplierSelect => page.GetByLabel(SELECT_BUSINESS_LABEL);

    public async Task GotoAsync(Guid qrToken) {
        string supplierVisitPath = string.Format(SUPPLIER_VISIT_PATH_TEMPLATE, qrToken);

        await page.GotoAsync(supplierVisitPath);
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task ConfirmVisitAsync(string supplierName, string supplierCode) {
        await SupplierSelect.SelectOptionAsync(new SelectOptionValue {
            Label = supplierName
        });
        await SupplierCodeInput.FillAsync(supplierCode);
        await ConfirmVisitButton.ClickAsync();
    }

    public async Task ConfirmVisitWithInvalidCodeAsync(string supplierName) {
        await ConfirmVisitAsync(supplierName, "WRONG-CODE");
        await Expect(SupplierErrorRegion).ToContainTextAsync(INVALID_BUSINESS_CODE_MESSAGE);
    }
}
