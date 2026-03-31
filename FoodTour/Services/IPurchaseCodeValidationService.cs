namespace FoodTour.Services;

public interface IPurchaseCodeValidationService {
    Task<PurchaseCodeValidationResult> ValidateAsync(PurchaseCodeValidationRequest request);
}