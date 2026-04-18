namespace FoodTour.Models;

public class Booking {
    public int Id { get; set; }

    [Required]
    public DateTime TourDate { get; set; }

    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PurchaseCode { get; set; } = string.Empty;    // Fareharbor booking reference

    [MaxLength(100)]
    public string? VoucherNumber { get; set; }  // Affiliate booking reference, optional

    public List<int> SelectedSupplierIds { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCancelled { get; set; } = false;
    public DateTime? CancelledAt { get; set; }
    public Guid QrToken { get; set; } = Guid.NewGuid();
    public int TotalGuests { get; set; }
    public bool IsConfirmed { get; set; }
    public string? CustomerName { get; set; }

    // Join collection for persistence of selected suppliers
    public ICollection<BookingSupplier> BookingSuppliers { get; set; } = [];
}
