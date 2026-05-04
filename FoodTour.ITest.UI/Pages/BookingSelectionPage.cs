namespace FoodTour.ITest.UI.Pages;

public sealed class BookingSelectionPage(IPage page) {
    private const string BOOKING_CONFIRMED_MODAL_HEADING = "Tasting Selection Complete";
    private const string BOOKING_FOUND_MESSAGE = "We've found your booking!";
    private const string BOOKING_REFERENCE_LABEL = "Booking Reference";
    private const string BOOKING_STATUS_ALERT = "Booking status";
    private const string CREATE_TOUR_BUTTON = "Create My Tour";
    private const string CUSTOMER_EMAIL_LABEL = "Email Address";
    private const string EMAIL_VALIDATION_MESSAGE = "Please enter a valid email address.";
    private const string FOOD_TOUR_PAGE_HEADING = "Weymouth Self-Guided Tasting Tour";
    private const string MAX_SELECTION_MESSAGE = "Maximum selections reached!";
    private const string PARTNER_CONFIRMATION_LINK = "Confirm email";
    private const string PARTNER_CONFIRMATION_PROMPT = "Partner booking confirmation";
    private const string SELECT_SUPPLIER_LABEL_TEMPLATE = "Select {0}";
    private const string VIEW_YOUR_TOUR_DETAILS_LINK = "View Your Tour Details";

    public ILocator BookingStatusMessage => page.GetByRole(AriaRole.Alert, new PageGetByRoleOptions {
        Name = BOOKING_STATUS_ALERT
    });

    public ILocator ConfirmedBookingModalHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKING_CONFIRMED_MODAL_HEADING
    });

    public ILocator CreateTourButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = CREATE_TOUR_BUTTON
    });

    public ILocator CustomerEmailInput => page.GetByLabel(CUSTOMER_EMAIL_LABEL);

    public ILocator EmailValidationMessage => page.GetByText(EMAIL_VALIDATION_MESSAGE);

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = FOOD_TOUR_PAGE_HEADING });

    public ILocator MaxSelectionMessage => page.GetByRole(AriaRole.Alert).Filter(new LocatorFilterOptions {
        HasText = MAX_SELECTION_MESSAGE
    });

    public ILocator PartnerConfirmationPrompt => page.GetByRole(AriaRole.Alert, new PageGetByRoleOptions {
        Name = PARTNER_CONFIRMATION_PROMPT
    });

    public ILocator PartnerConfirmationLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = PARTNER_CONFIRMATION_LINK
    });

    public ILocator PurchaseCodeInput => page.GetByLabel(BOOKING_REFERENCE_LABEL);

    public ILocator ViewYourTourDetailsButton => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = VIEW_YOUR_TOUR_DETAILS_LINK
    });

    public async Task GotoAsync(Guid? qrToken = null) {
        string path = qrToken.HasValue
            ? $"/?qrToken={qrToken.Value}"
            : "/";

        await page.GotoAsync(path);
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task EnterBookingLookupDetailsAsync(string customerEmail, string purchaseCode) {
        await CustomerEmailInput.FillAsync(customerEmail);
        await PurchaseCodeInput.FillAsync(purchaseCode);
    }

    public async Task EnterBookingDetailsAsync(string customerEmail, string purchaseCode) {
        await EnterBookingLookupDetailsAsync(customerEmail, purchaseCode);

        await Expect(BookingStatusMessage).ToContainTextAsync(BOOKING_FOUND_MESSAGE);
    }

    public async Task EnterConfirmedBookingDetailsAsync(string customerEmail, string purchaseCode) {
        await EnterBookingLookupDetailsAsync(customerEmail, purchaseCode);

        await Expect(ConfirmedBookingModalHeading).ToBeVisibleAsync();
    }

    public async Task SelectSuppliersAsync(IEnumerable<string> supplierNames) {
        foreach (string supplierName in supplierNames) {
            await ToggleSupplierAsync(supplierName);
            await Expect(GetSupplierCard(supplierName)).ToHaveAttributeAsync("aria-checked", "true");
        }

        await Expect(CreateTourButton).ToBeEnabledAsync();
    }

    public async Task<int> GetSelectedSupplierCountAsync() {
        return await page.Locator(".supplier-checkbox:checked").CountAsync();
    }

    public ILocator GetSupplierOpeningTimes(string supplierName) {
        return GetSupplierCard(supplierName).Locator("[data-opening-times-text]");
    }

    public async Task ToggleSupplierAsync(string supplierName) {
        var supplierCard = GetSupplierCard(supplierName);

        await supplierCard.FocusAsync();
        await page.Keyboard.PressAsync("Space");
    }

    public async Task SubmitSelectionAsync() {
        await CreateTourButton.ClickAsync();
    }

    private ILocator GetSupplierCard(string supplierName) {
        string selectSupplierLabel = string.Format(SELECT_SUPPLIER_LABEL_TEMPLATE, supplierName);

        return page.GetByRole(AriaRole.Checkbox, new PageGetByRoleOptions {
            Name = selectSupplierLabel,
            Exact = true
        });
    }
}
