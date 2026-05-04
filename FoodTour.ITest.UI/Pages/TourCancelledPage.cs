namespace FoodTour.ITest.UI.Pages;

public sealed class TourCancelledPage(IPage page) {
    private const string BOOKING_DETAILS_REGION = "Booking Details";
    private const string TOUR_CANCELLED_HEADING = "Tour Cancelled";

    public ILocator BookingDetails => page.GetByRole(AriaRole.Region, new PageGetByRoleOptions {
        Name = BOOKING_DETAILS_REGION
    });

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = TOUR_CANCELLED_HEADING
    });
}
