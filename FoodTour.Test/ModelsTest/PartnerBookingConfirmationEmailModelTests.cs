namespace FoodTour.Test.Models;

public class PartnerBookingConfirmationEmailModelTests {
    [Fact]
    public void PartnerBookingConfirmationEmailModel_ShouldInitializeWithDefaultValues() {
        var partnerBookingConfirmationEmailModel = new PartnerBookingConfirmationEmailModel();
        partnerBookingConfirmationEmailModel.CustomerEmail.ShouldBeEmpty();
        partnerBookingConfirmationEmailModel.BookingReference.ShouldBeEmpty();
        partnerBookingConfirmationEmailModel.ValidationUrl.ShouldBeEmpty();
        partnerBookingConfirmationEmailModel.PrivacyPolicyUrl.ShouldBeEmpty();
        partnerBookingConfirmationEmailModel.Subject.ShouldBeEmpty();
    }
}
