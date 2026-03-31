namespace FoodTour.Models;

public record PurchaseCodeValidationResult {
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static PurchaseCodeValidationResult Success() => new() { IsValid = true };
    public static PurchaseCodeValidationResult Failure(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
}