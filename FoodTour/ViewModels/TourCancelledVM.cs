namespace FoodTour.Models;

public class TourCancelledVM {
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime TourDate { get; set; }
    public string PurchaseCode { get; set; } = string.Empty;
}
