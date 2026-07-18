namespace FoodTour.Services;

public class BookingValidationService(FoodTourContext context) : IBookingValidationService {
    private const int MAX_SUPPLIER_SELECTIONS = 5;


    private static readonly EmailAddressAttribute emailValidator = new();

    public async Task<BookingValidationResult> ValidateAsync(BookingValidationRequest request) {
        var result = new BookingValidationResult();

        result.IsTourDateValid = ValidateTourDate(request.TourDate, result);
        result.IsCustomerEmailValid = ValidateCustomerEmail(request.CustomerEmail, result);
        // Skip if not provided
        result.IsCustomerCountValid = !request.CustomerCount.HasValue || ValidateCustomerCount(request.CustomerCount, result);
        result.AreSelectedSuppliersValid = request.SelectedSupplierIds == null || await ValidateSelectedSuppliersAsync(request, result);
        result.IsCustomerNameValid = string.IsNullOrWhiteSpace(request.CustomerName) || IsCustomerNameValid(request.CustomerName);
        return result;
    }

    public bool IsTourDateValid(DateTime? tourDate) {
        if (!tourDate.HasValue) {
            Log.Debug("Tour date is null");
            return false;
        }

        return IsValidTourDate(tourDate.Value);
    }

    public bool IsCustomerEmailValid(string? customerEmail) {
        return !string.IsNullOrWhiteSpace(customerEmail) && emailValidator.IsValid(customerEmail);
    }

    public bool IsCustomerCountValid(int? customerCount) {
        return customerCount.HasValue && customerCount.Value > 0;
    }

    public bool IsCustomerNameValid(string? customerName) {
        return !string.IsNullOrWhiteSpace(customerName) && customerName.Length < 1024;
    }

    private static bool ValidateTourDate(DateTime? tourDate, BookingValidationResult result) {
        if (!tourDate.HasValue) {
            result.IsTourDateNull = true;
            result.AddError("Tour date is required");
            return false;
        }

        if (!IsValidTourDate(tourDate.Value)) {
            result.AddError($"Tour date {tourDate.Value:yyyy-MM-dd} must be in the future");
            return false;
        }

        return true;
    }

    private static bool ValidateCustomerEmail(string? customerEmail, BookingValidationResult result) {
        if (string.IsNullOrWhiteSpace(customerEmail)) {
            result.IsCustomerEmailEmpty = true;
            result.AddError("Customer email is required");
            return false;
        }

        if (!emailValidator.IsValid(customerEmail)) {
            result.AddError($"Customer email '{customerEmail}' is not in a valid format");
            return false;
        }

        return true;
    }

    private static bool ValidateCustomerCount(int? customerCount, BookingValidationResult result) {
        if (!customerCount.HasValue || customerCount.Value <= 0) {
            result.AddError($"Customer count must be greater than 0. Provided value: {customerCount?.ToString() ?? "null"}");
            Log.Debug("Customer count is invalid. Value: {CustomerCount}", customerCount);
            return false;
        }

        return true;
    }

    private async Task<bool> ValidateSelectedSuppliersAsync(BookingValidationRequest request, BookingValidationResult result) {
        var selectedSupplierIds = request.SelectedSupplierIds;
        if (selectedSupplierIds == null || selectedSupplierIds.Count == 0) {
            result.AddError("At least one supplier must be selected");
            return false;
        }

        // Determine required selections based on provided tasting count (default to MAX_SUPPLIER_SELECTIONS)
        int requiredSelections = request.TastingCount ?? MAX_SUPPLIER_SELECTIONS;

        if (selectedSupplierIds.Count != requiredSelections) {
            result.AddError($"Exactly {requiredSelections} suppliers must be selected. Currently selected: {selectedSupplierIds.Count}");
            return false;
        }

        // If we have the right count, check if they actually exist in the database
        // This validation is done here for the API controller, but also done again 
        // in HomeController for better error messaging
        try {
            var existingSupplierIds = await context.Suppliers
                .Where(s => selectedSupplierIds.Contains(s.Id) && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            if (existingSupplierIds.Count != selectedSupplierIds.Count) {
                var missingIds = selectedSupplierIds.Except(existingSupplierIds).ToList();
                result.AddError($"One or more selected suppliers do not exist or are inactive. Missing supplier IDs: [{string.Join(", ", missingIds)}]");
                Log.Warning("One or more selected supplier IDs do not exist in the database. Provided: {ProvidedIds}, Existing: {ExistingIds}",
                    selectedSupplierIds, existingSupplierIds);
                return false;
            }

            return true;
        } catch (Exception ex) {
            result.AddError($"Failed to validate selected suppliers: {ex.Message}");
            Log.Error(ex, "Failed to validate selected suppliers against database.");
            return false;
        }
    }

    private static bool IsValidTourDate(DateTime date) {
        return date > DateTime.Today;
    }
}