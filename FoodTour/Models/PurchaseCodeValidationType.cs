namespace FoodTour.Models;

public enum PurchaseCodeValidationType {
    MustExistAndBeActive,      // HomeController scenario - must exist in PurchaseCodes table and be active
    MustNotExist,              // API newbooking scenario - must not exist in Bookings table
    MustExist,                 // Future updatebooking scenario - must exist in Bookings table
    MustExistForCancellation   // Future cancelledbooking scenario - must exist in Bookings table and not be cancelled
}