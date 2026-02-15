namespace FoodTour;

public class DataProtectionKeyContext(DbContextOptions<DataProtectionKeyContext> options) : DbContext(options), IDataProtectionKeyContext {
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
}
