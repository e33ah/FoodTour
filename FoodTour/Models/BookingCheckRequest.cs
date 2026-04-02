namespace FoodTour.Models;

public class BookingCheckRequest {
    public string CustomerEmail { get; set; } = string.Empty;
    public string PurchaseCode { get; set; } = string.Empty;
}
