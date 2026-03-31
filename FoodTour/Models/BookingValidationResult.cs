namespace FoodTour.Models;

public class BookingValidationResult {
    public bool IsTourDateValid { get; set; }
    public bool IsCustomerEmailValid { get; set; }
    public bool IsCustomerEmailConfirmationValid { get; set; }
    public bool IsCustomerCountValid { get; set; }
    public bool AreSelectedSuppliersValid { get; set; }

    // Specific validation failure reasons
    public bool IsTourDateNull { get; set; }
    public bool IsCustomerEmailEmpty { get; set; }
    public bool IsCustomerEmailConfirmationEmpty { get; set; }

    public bool IsValid => IsTourDateValid 
                        && IsCustomerEmailValid 
                        && IsCustomerEmailConfirmationValid 
                        && IsCustomerCountValid 
                        && AreSelectedSuppliersValid;

    public List<string> ErrorMessages { get; set; } = [];

    public void AddError(string errorMessage) {
        ErrorMessages.Add(errorMessage);
    }
}