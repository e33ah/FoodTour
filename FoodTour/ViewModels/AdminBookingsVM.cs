namespace FoodTour.ViewModels;

public class AdminBookingsVM {
    public IReadOnlyList<Booking> Bookings { get; set; } = [];
    public string? ActiveFilter { get; set; }
}
