namespace FoodTour.Services;

public class PurchaseCodeValidationService(FoodTourContext context) : IPurchaseCodeValidationService {

    public async Task<PurchaseCodeValidationResult> ValidateAsync(PurchaseCodeValidationRequest request) {
        if (string.IsNullOrWhiteSpace(request.PurchaseCode)) {
            Log.Debug("Purchase code is null or whitespace");
            return PurchaseCodeValidationResult.Failure("Purchase code is required.");
        }

        return request.ValidationType switch {
            PurchaseCodeValidationType.MustNotExist => await ValidateMustNotExistAsync(request.PurchaseCode),
            PurchaseCodeValidationType.MustExist => await ValidateMustExistAsync(request.PurchaseCode),
            PurchaseCodeValidationType.MustExistForCancellation => await ValidateMustExistForCancellationAsync(request.PurchaseCode),
            _ => PurchaseCodeValidationResult.Failure("Unknown validation type.")
        };
    }

    private async Task<PurchaseCodeValidationResult> ValidateMustNotExistAsync(string purchaseCode) {
        bool bookingExists = await context.Bookings
            .AnyAsync(b => b.PurchaseCode == purchaseCode);
        Log.Debug($"Purchase code must not exist. Purchase code '{purchaseCode}' existence in bookings: {bookingExists}");
        return !bookingExists
            ? PurchaseCodeValidationResult.Success()
            : PurchaseCodeValidationResult.Failure("This purchase code has already been used.");
    }

    private async Task<PurchaseCodeValidationResult> ValidateMustExistAsync(string purchaseCode) {
        bool bookingExists = await context.Bookings
            .AnyAsync(b => b.PurchaseCode == purchaseCode);
        Log.Debug($"Purchase code must exist. Purchase code: '{purchaseCode}' existence in bookings: {bookingExists}");

        return bookingExists
            ? PurchaseCodeValidationResult.Success()
            : PurchaseCodeValidationResult.Failure("Booking not found for the provided purchase code.");
    }

    private async Task<PurchaseCodeValidationResult> ValidateMustExistForCancellationAsync(string purchaseCode) {
        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.PurchaseCode == purchaseCode);
        Log.Debug($"Purchase code must exist for cancellation. Purchase code: '{purchaseCode}' existence in bookings: {(booking != null ? "Found" : "Not Found")}");
        if (booking == null) {
            return PurchaseCodeValidationResult.Failure("Booking not found for the provided purchase code.");
        }

        if (booking.IsCancelled) {
            Log.Debug($"Booking with purchase code '{purchaseCode}' is already cancelled.");
            return PurchaseCodeValidationResult.Failure("This booking has already been cancelled.");
        }

        return PurchaseCodeValidationResult.Success();
    }
}