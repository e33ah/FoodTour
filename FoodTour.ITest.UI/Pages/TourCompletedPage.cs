namespace FoodTour.ITest.UI.Pages;

public sealed class TourCompletedPage(IPage page) {
    private const string TOUR_COMPLETED_HEADING = "Tour complete!";

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = TOUR_COMPLETED_HEADING
    });
}
