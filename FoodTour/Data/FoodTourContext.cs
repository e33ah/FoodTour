namespace FoodTour;

public class FoodTourContext(DbContextOptions<FoodTourContext> options) : DbContext(options) {
    public DbSet<Supplier> Suppliers { get; set; } = default!;
}
