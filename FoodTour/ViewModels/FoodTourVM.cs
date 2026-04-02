namespace FoodTour.Models;

public class FoodTourVM {
    public List<Supplier> Suppliers { get; set; } = [];
    public int MaxSelections { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierDescription { get; set; } = string.Empty;
    public string SupplierAddress { get; set; } = string.Empty;
    public string SupplierOpeningTimes { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a date for your tour")]
    [DataType(DataType.Date)]
    public DateTime? TourDate { get; set; }

    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your purchase code")]
    public string PurchaseCode { get; set; } = string.Empty;
}
