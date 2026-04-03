namespace FoodTour.Services;

public interface IBookingUpdateService {
    Task UpdateBookingSupplierSelectionsAsync(Booking booking, List<int> selectedSupplierIds);
}
