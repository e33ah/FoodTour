namespace FoodTour.Models;

public class FoodTourVM {
    public List<Supplier> Suppliers { get; set; } = [];
    public int MaxSelections { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierDescription { get; set; } = string.Empty;
    public string SupplierAddress { get; set; } = string.Empty;
    public string SupplierOpeningTimes { get; set; } = string.Empty;
    public List<int> SelectedSuppliers { get; set; } = [];
    public string CustomerEmail { get; set; } = string.Empty;
    public string PurchaseCode { get; set; } = string.Empty;
    public string? NoSuppliersMessage { get; set; }
    public string? InsufficientSuppliersWarning { get; set; }
}
