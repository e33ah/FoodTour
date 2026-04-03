namespace FoodTour.Models;

public class SupplierVisitVM {
    public int BookingId { get; set; }
    public Guid QrToken { get; set; }
    public IReadOnlyList<Supplier> Suppliers { get; set; } = [];
    public DateTime TourDate { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public int SelectedSupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public int TotalGuests { get; set; }
}
