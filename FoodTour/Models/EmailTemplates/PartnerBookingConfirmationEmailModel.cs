namespace FoodTour.Models;

public class PartnerBookingConfirmationEmailModel {
    public string CustomerEmail { get; set; } = string.Empty;
    public string BookingReference { get; set; } = string.Empty;
    public string ValidationUrl { get; set; } = string.Empty;
    public string PrivacyPolicyUrl { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}
