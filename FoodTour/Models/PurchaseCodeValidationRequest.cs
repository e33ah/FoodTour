namespace FoodTour.Models;

public record PurchaseCodeValidationRequest {
    public required string PurchaseCode { get; init; }
    public required PurchaseCodeValidationType ValidationType { get; init; }
}