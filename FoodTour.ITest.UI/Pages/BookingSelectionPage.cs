namespace FoodTour.ITest.UI.Pages;

public sealed class BookingSelectionPage(IPage page) {
    private const string BOOKING_CONFIRMED_MODAL_HEADING = "Tasting Selection Complete";
    private const string BOOKING_CONFIRMATION_HEADING = "Booking Confirmed";
    private const string BOOKING_FOUND_MESSAGE = "We've found your booking!";
    private const string BOOKING_SUMMARY_HEADING = "Booking Summary";
    private const string CUSTOMER_EMAIL_INPUT = "#customerEmail";
    private const string CREATE_TOUR_BUTTON = "Create My Tour";
    private const string FOOD_TOUR_PAGE_HEADING = "Weymouth Self-Guided Tasting Tour";
    private const string PURCHASE_CODE_INPUT = "#purchaseCode";
    private const string SELECTED_SUPPLIER_LIST_ITEM_LOCATOR = "ul.list-group li ul li";
    private const string SUPPLIER_CARD_LOCATOR = ".supplier-card";
    private const string TOUR_SUMMARY_HEADING = "Tour Summary";
    private const string VIEW_TOUR_DETAILS_LINK = "View Tour Details";
    private const string VIEW_YOUR_TOUR_DETAILS_LINK = "View Your Tour Details";

    public ILocator BookingStatusMessage => page.Locator("#bookingStatusMessage");

    public ILocator ConfirmedBookingModalHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKING_CONFIRMED_MODAL_HEADING
    });

    public ILocator BookingConfirmationHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKING_CONFIRMATION_HEADING
    });

    public ILocator ConfirmBookingButton => page.Locator("#btnConfirm");

    public ILocator CreateTourButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = CREATE_TOUR_BUTTON
    });

    public ILocator CustomerEmailInput => page.Locator(CUSTOMER_EMAIL_INPUT);

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = FOOD_TOUR_PAGE_HEADING });

    public ILocator PurchaseCodeInput => page.Locator(PURCHASE_CODE_INPUT);

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

    public async Task SelectSuppliersAsync(IEnumerable<int> supplierIds) {
        foreach (int supplierId in supplierIds) {
            string supplierCardLocator = $"{SUPPLIER_CARD_LOCATOR}[data-supplier-id=\"{supplierId}\"]";
            var supplierCard = page.Locator(supplierCardLocator);

            await supplierCard.ClickAsync();
        }

        await Expect(CreateTourButton).ToBeEnabledAsync();
    }

    public ILocator SelectedSupplier(string supplierName) {
        return page.Locator(SELECTED_SUPPLIER_LIST_ITEM_LOCATOR).Filter(new LocatorFilterOptions {
            HasText = supplierName
        });
    }

    public async Task SubmitSelectionAsync() {
        await CreateTourButton.ClickAsync();
    }
}
