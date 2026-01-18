namespace FoodTour;

public class FoodTourContext(DbContextOptions<FoodTourContext> options) : DbContext(options) {
    public DbSet<Supplier> Suppliers { get; set; } = default!;
    public DbSet<Booking> Bookings { get; set; } = default!;
    public DbSet<BookingSupplier> BookingSuppliers { get; set; } = default!;
}
