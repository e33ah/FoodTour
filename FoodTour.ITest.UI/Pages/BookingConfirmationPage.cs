namespace FoodTour.ITest.UI.Pages;

public sealed class BookingConfirmationPage(IPage page) {
    private const string BOOKING_CONFIRMATION_HEADING = "Booking Confirmed";
    private const string VIEW_TOUR_DETAILS_LINK = "View Tour Details";

    public ILocator BookingConfirmationWarning => page.GetByRole(AriaRole.Alert);

    public ILocator BookingConfirmationHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = BOOKING_CONFIRMATION_HEADING
    });

    public ILocator ViewTourDetailsButton => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions {
        Name = VIEW_TOUR_DETAILS_LINK
    });
}
