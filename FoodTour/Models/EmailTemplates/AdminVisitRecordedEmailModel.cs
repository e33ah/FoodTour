namespace FoodTour.Models;

public class AdminVisitRecordedEmailModel {
    public required string BookingRef { get; init; }
    public required string CustomerEmail { get; init; }
    public required string TourDate { get; init; }
    public required string TotalGuests { get; init; }
    public required string SupplierName { get; init; }
    public required string ClaimedAt { get; init; }
    public required string Subject { get; init; }
}
