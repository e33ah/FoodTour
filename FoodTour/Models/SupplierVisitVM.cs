namespace FoodTour.Models;

public class SupplierVisitVM {
    public IReadOnlyList<Supplier> Suppliers { get; set; } = [];
    public DateTime TourDate { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public int? SelectedSupplierId { get; set; }

}
