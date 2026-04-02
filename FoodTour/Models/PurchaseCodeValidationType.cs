namespace FoodTour.Models;

public enum PurchaseCodeValidationType {
    MustNotExist,              // API newbooking scenario - must not exist in Bookings table
    MustExist,                 // API updatebooking scenario - must exist in Bookings table
    MustExistForCancellation   // API cancelledbooking scenario - must exist in Bookings table and not be cancelled
}