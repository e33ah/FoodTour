namespace FoodTour.Models;

public class Visit {
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public bool IsClaimed { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public int TastingExperiencesClaimed { get; set; }

    public bool IsSupplierReimbursed { get; set; }
    public DateTime? ReimbursedAt { get; set; }
}
