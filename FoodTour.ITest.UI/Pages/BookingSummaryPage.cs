namespace FoodTour.ITest.UI.Pages;

public sealed class BookingSummaryPage(IPage page) {
    private const string BOOKING_SUMMARY_HEADING = "Booking Summary";
    private const string CONFIRM_BOOKING_BUTTON = "Confirm Booking";
    private const string SELECTED_TASTING_EXPERIENCES_LIST = "Selected tasting experiences";

    public ILocator ConfirmBookingButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = CONFIRM_BOOKING_BUTTON
    });

    public ILocator SummaryHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKING_SUMMARY_HEADING
    });

    public ILocator SelectedSupplier(string supplierName) {
        var selectedTastingExperiencesList = page.GetByRole(AriaRole.List, new PageGetByRoleOptions {
            Name = SELECTED_TASTING_EXPERIENCES_LIST
        });

        return selectedTastingExperiencesList.GetByRole(AriaRole.Listitem).Filter(new LocatorFilterOptions {
            HasText = supplierName
        });
    }
}
