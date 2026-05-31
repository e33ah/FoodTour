namespace FoodTour.Models;

public class AdminBookingDetailVM {
    public Booking Booking { get; set; } = null!;
    public IReadOnlyList<Visit> Visits { get; set; } = [];
    public IReadOnlyList<Supplier> SelectedSuppliers { get; set; } = [];
}
