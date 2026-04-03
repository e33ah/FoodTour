namespace FoodTour.Services;

public class BookingUpdateService(FoodTourContext context) : IBookingUpdateService {
    public async Task UpdateBookingSupplierSelectionsAsync(Booking booking, List<int> selectedSupplierIds) {
        booking.SelectedSupplierIds = selectedSupplierIds;

        await RemoveOldBookingSuppliersAsync(booking.Id);
        await AddNewBookingSuppliersAsync(booking, selectedSupplierIds);
        await RemoveOldVisitsAsync(booking.Id);
        await AddNewVisitsAsync(booking, selectedSupplierIds);

        await context.SaveChangesAsync();
        Log.Information("Booking {BookingId}: Updated for {CustomerEmail} on {TourDate:dd/MM/yyyy} with {SupplierCount} suppliers.", booking.Id, booking.CustomerEmail, booking.TourDate, selectedSupplierIds.Count);
    }

    private async Task RemoveOldBookingSuppliersAsync(int bookingId) {
        var oldBookingSuppliers = await context.BookingSuppliers
            .Where(bs => bs.BookingId == bookingId)
            .ToListAsync();
        context.BookingSuppliers.RemoveRange(oldBookingSuppliers);
    }

    private async Task AddNewBookingSuppliersAsync(Booking booking, List<int> selectedSupplierIds) {
        var newBookingSuppliers = selectedSupplierIds.Select(id =>
            new BookingSupplier {
                Booking = booking,
                SupplierId = id
            }).ToList();

        await context.BookingSuppliers.AddRangeAsync(newBookingSuppliers);
    }

    private async Task RemoveOldVisitsAsync(int bookingId) {
        var oldVisits = await context.Visits
            .Where(v => v.BookingId == bookingId)
            .ToListAsync();
        context.Visits.RemoveRange(oldVisits);
    }

    private async Task AddNewVisitsAsync(Booking booking, List<int> selectedSupplierIds) {
        var newVisits = selectedSupplierIds.Select(id =>
            new Visit {
                Booking = booking,
                SupplierId = id,
                IsClaimed = false
            }).ToList();

        await context.Visits.AddRangeAsync(newVisits);
    }
}
