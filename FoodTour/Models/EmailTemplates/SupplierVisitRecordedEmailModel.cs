namespace FoodTour.Models;

public class SupplierVisitRecordedEmailModel {
    public required string SupplierName { get; init; }
    public required string BookingRef { get; init; }
    public required string TourDate { get; init; }
    public required string TotalGuests { get; init; }
    public required string ClaimedAt { get; init; }
    public required string SupportUrl { get; init; }
    public required string Subject { get; init; }
}
