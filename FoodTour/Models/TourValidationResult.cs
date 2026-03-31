namespace FoodTour.Models;

public class TourValidationResult {
    public bool IsTourDateValid { get; set; }
    public bool IsCustomerEmailValid { get; set; }
    public bool IsPurchaseCodeValid { get; set; }
    public bool IsTourGuestCountValid { get; set; }

    public bool IsValid => IsTourDateValid && IsCustomerEmailValid && IsPurchaseCodeValid && IsTourGuestCountValid;
}
