namespace FoodTour.Models;

public class FoodTourVM {
    public List<Supplier> Suppliers { get; set; } = [];
    public int MaxSelections { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierDescription { get; set; } = string.Empty;
    public string SupplierAddress { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please select a date for your tour")]
    [DataType(DataType.Date)]
    public DateTime? TourDate { get; set; }
}
