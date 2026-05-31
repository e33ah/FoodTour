namespace FoodTour.Models;

public class PartnerBookingConfirmationVM {
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    public string BookingReference { get; set; } = string.Empty;

    public bool EmailSent { get; set; }
    public string? LinkErrorMessage { get; set; }
}
