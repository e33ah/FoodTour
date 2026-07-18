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
    public bool RequiresEmailConfirmation { get; set; } // True when a voucher booking may still be using a pseudo email and needs the customer to confirm their real one

    // Number of tastings associated with this booking (e.g., 5 or 3)
    public int TastingCount { get; set; } = 5;

    // Join collection for persistence of selected suppliers
    public ICollection<BookingSupplier> BookingSuppliers { get; set; } = [];
}
