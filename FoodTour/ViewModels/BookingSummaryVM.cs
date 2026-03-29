namespace FoodTour.Models;

public class BookingSummaryVM {
    public DateTime TourDate { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string PurchaseCode { get; set; } = string.Empty;
    public List<int> SelectedSupplierIds { get; set; } = [];
    public List<Supplier> SelectedSuppliers { get; set; } = [];
    public Guid QrToken { get; set; }
}
