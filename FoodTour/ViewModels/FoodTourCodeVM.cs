namespace FoodTour.Models;

public class FoodTourCodeVM {
    public DateTime TourDate { get; set; }
    public List<Supplier> TourSuppliers { get; set; } = [];
    public Guid QrToken { get; set; } = Guid.Empty;
    public bool AllVisitsClaimed { get; set; } = false;
}
