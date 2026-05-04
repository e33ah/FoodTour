namespace FoodTour.ITest.UI.TestData;

public sealed class FoodTourSeedData {
    public required IReadOnlyList<SeededSupplier> Suppliers { get; init; }
    public required IReadOnlyList<SeededSupplier> ActiveSuppliers { get; init; }
    public required IReadOnlyList<SeededBooking> Bookings { get; init; }
    public required int RequiredSupplierSelectionCount { get; init; }
    public required SeededSupplier InactiveSupplier { get; init; }
    public required SeededBooking BookingSelectionBooking { get; init; }
    public required SeededBooking ConfirmedBooking { get; init; }
    public required SeededBooking CancelledBooking { get; init; }
    public required SeededBooking PastBooking { get; init; }
    public required SeededBooking SupplierVisitBooking { get; init; }
    public required SeededBooking PartnerConfirmationBooking { get; init; }
    public required IReadOnlyList<SeededVisit> SupplierVisitBookingVisits { get; init; }

    public int SupplierCount => Suppliers.Count;

    public int ActiveSupplierCount => ActiveSuppliers.Count;

    public int BookingCount => Bookings.Count;

    public int VisitCount => SupplierVisitBookingVisits.Count;
}

public sealed record SeededSupplier(int Id, string Name, string ConfirmationCode, bool IsActive);

public sealed record SeededBooking(
    int Id,
    string CustomerEmail,
    string PurchaseCode,
    string? VoucherNumber,
    Guid QrToken,
    DateTime TourDate,
    bool IsConfirmed,
    bool IsCancelled,
    bool RequiresEmailConfirmation,
    int TotalGuests);

public sealed record SeededVisit(int Id, int BookingId, int SupplierId, bool IsClaimed);
