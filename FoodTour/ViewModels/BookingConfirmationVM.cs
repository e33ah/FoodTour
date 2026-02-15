namespace FoodTour.Models;

public class BookingConfirmationVM {
    public DateTime TourDate { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string PurchaseCode { get; set; } = string.Empty;
    public List<int> SelectedSupplierIds { get; set; } = [];
    public int BookingId { get; set; }
    public Guid QrToken { get; set; } = Guid.Empty;
}
