namespace FoodTour.ITest.UI.Pages;

public sealed class VisitConfirmationPage(IPage page) {
    private const string SUCCESS_HEADING = "Success!";

    public ILocator PreparationAlert => page.GetByRole(AriaRole.Alert);

    public ILocator SuccessHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = SUCCESS_HEADING
    });
}
