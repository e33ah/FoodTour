namespace FoodTour.Models;

public class Booking {
    public int Id { get; set; }
    [Required]
    public DateTime TourDate { get; set; }
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;
    [Required]
    public string PurchaseCode { get; set; } = string.Empty;
    public List<int> SelectedSupplierIds { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCancelled { get; set; } = false;
    public DateTime? CancelledAt { get; set; }
    public Guid QrToken { get; set; } = Guid.NewGuid();
    public int TotalGuests { get; set; }
    public bool IsConfirmed { get; set; }

    // Join collection for persistence of selected suppliers
    public ICollection<BookingSupplier> BookingSuppliers { get; set; } = [];
}
