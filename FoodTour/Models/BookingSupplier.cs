namespace FoodTour.Models;

public class BookingSupplier {
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
}
