namespace FoodTour.ITest.UI.Pages;

public sealed class BookingSelectionPage(IPage page) {
    private const string BOOKING_CONFIRMED_MODAL_HEADING = "Tasting Selection Complete";
    private const string BOOKING_CONFIRMATION_HEADING = "Booking Confirmed";
    private const string BOOKING_FOUND_MESSAGE = "We've found your booking!";
    private const string BOOKING_SUMMARY_HEADING = "Booking Summary";
    private const string BOOKING_REFERENCE_LABEL = "Booking Reference";
    private const string BOOKING_STATUS_ALERT = "Booking status";
    private const string CREATE_TOUR_BUTTON = "Create My Tour";
    private const string CUSTOMER_EMAIL_LABEL = "Email Address";
    private const string CONFIRM_BOOKING_BUTTON = "Confirm Booking";
    private const string FOOD_TOUR_PAGE_HEADING = "Weymouth Self-Guided Tasting Tour";
    private const string SELECT_SUPPLIER_LABEL_TEMPLATE = "Select {0}";
    private const string SELECTED_TASTING_EXPERIENCES_LIST = "Selected tasting experiences";
    private const string TOUR_SUMMARY_HEADING = "Tour Summary";
    private const string VIEW_TOUR_DETAILS_LINK = "View Tour Details";
    private const string VIEW_YOUR_TOUR_DETAILS_LINK = "View Your Tour Details";

    public ILocator BookingStatusMessage => page.GetByRole(AriaRole.Alert, new PageGetByRoleOptions {
        Name = BOOKING_STATUS_ALERT
    });

    public ILocator ConfirmedBookingModalHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKING_CONFIRMED_MODAL_HEADING
    });

    public ILocator BookingConfirmationHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKING_CONFIRMATION_HEADING
    });

    public ILocator ConfirmBookingButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = CONFIRM_BOOKING_BUTTON
    });

    public ILocator CreateTourButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = CREATE_TOUR_BUTTON
    });

    public ILocator CustomerEmailInput => page.GetByLabel(CUSTOMER_EMAIL_LABEL);

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = FOOD_TOUR_PAGE_HEADING });

    public ILocator PurchaseCodeInput => page.GetByLabel(BOOKING_REFERENCE_LABEL);

    public ILocator SummaryHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKING_SUMMARY_HEADING
    });

    public ILocator TourSummaryHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = TOUR_SUMMARY_HEADING
    });

    public ILocator ViewTourDetailsButton => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = VIEW_TOUR_DETAILS_LINK
    });

    public ILocator ViewYourTourDetailsButton => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = VIEW_YOUR_TOUR_DETAILS_LINK
    });

    public async Task GotoAsync() {
        await page.GotoAsync("/");
        await Expect(PageHeading).ToBeVisibleAsync();
    }

    public async Task EnterBookingDetailsAsync(string customerEmail, string purchaseCode) {
        await CustomerEmailInput.FillAsync(customerEmail);
        await PurchaseCodeInput.FillAsync(purchaseCode);

        await Expect(BookingStatusMessage).ToContainTextAsync(BOOKING_FOUND_MESSAGE);
    }

    public async Task EnterConfirmedBookingDetailsAsync(string customerEmail, string purchaseCode) {
        await CustomerEmailInput.FillAsync(customerEmail);
        await PurchaseCodeInput.FillAsync(purchaseCode);

        await Expect(ConfirmedBookingModalHeading).ToBeVisibleAsync();
    }

    public async Task SelectSuppliersAsync(IEnumerable<string> supplierNames) {
        foreach (string supplierName in supplierNames) {
            string selectSupplierLabel = string.Format(SELECT_SUPPLIER_LABEL_TEMPLATE, supplierName);
            var supplierCard = page.GetByRole(AriaRole.Checkbox, new PageGetByRoleOptions {
                Name = selectSupplierLabel,
                Exact = true
            });

            await supplierCard.FocusAsync();
            await page.Keyboard.PressAsync("Space");
            await Expect(supplierCard).ToHaveAttributeAsync("aria-checked", "true");
        }

        await Expect(CreateTourButton).ToBeEnabledAsync();
    }

    public ILocator SelectedSupplier(string supplierName) {
        var selectedTastingExperiencesList = page.GetByRole(AriaRole.List, new PageGetByRoleOptions {
            Name = SELECTED_TASTING_EXPERIENCES_LIST
        });

        return selectedTastingExperiencesList.GetByRole(AriaRole.Listitem).Filter(new LocatorFilterOptions {
            HasText = supplierName
        });
    }

    public async Task SubmitSelectionAsync() {
        await CreateTourButton.ClickAsync();
    }
}
