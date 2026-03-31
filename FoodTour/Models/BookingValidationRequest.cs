namespace FoodTour.Models;

public class BookingValidationRequest {
    public DateTime? TourDate { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerEmailConfirmation { get; set; }
    public int? CustomerCount { get; set; }
    public List<int>? SelectedSupplierIds { get; set; }
}