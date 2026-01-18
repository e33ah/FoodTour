namespace FoodTour.Models;

public class ConfirmationVM {
    public DateTime TourDate { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string PurchaseCode { get; set; } = string.Empty;
    public List<int> SelectedSupplierIds { get; set; } = [];
}
