namespace FoodTour.ITest.UI.Pages;

public sealed class FoodTourQRCodePage(IPage page) {
    private const string TOUR_SUMMARY_HEADING = "Tour Summary";

    public ILocator TourSummaryHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = TOUR_SUMMARY_HEADING
    });
}
