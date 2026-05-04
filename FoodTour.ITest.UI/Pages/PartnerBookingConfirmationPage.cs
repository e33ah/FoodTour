namespace FoodTour.ITest.UI.Pages;

public sealed class PartnerBookingConfirmationPage(IPage page) {
    private const string BOOKING_REFERENCE_LABEL = "Booking Reference";
    private const string CONFIRM_YOUR_EMAIL_HEADING = "Confirm your email";
    private const string EMAIL_LABEL = "Email";
    private const string SEND_CONFIRMATION_EMAIL_BUTTON = "Send confirmation email";

    public ILocator BookingReferenceInput => page.GetByLabel(BOOKING_REFERENCE_LABEL);

    public ILocator CustomerEmailInput => page.GetByLabel(EMAIL_LABEL);

    public ILocator EmailSentAlert => page.GetByRole(AriaRole.Alert);

    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions {
        Name = CONFIRM_YOUR_EMAIL_HEADING
    });

    public ILocator SendConfirmationEmailButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions {
        Name = SEND_CONFIRMATION_EMAIL_BUTTON
    });
}
