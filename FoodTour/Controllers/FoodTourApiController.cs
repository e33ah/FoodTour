namespace FoodTour.Controllers;

[ApiController]
[Route("api/foodtour")]
public partial class FoodTourApiController(FoodTourContext context, IBookingValidationService bookingValidationService, IPurchaseCodeValidationService purchaseCodeValidationService) : ControllerBase {
    private const string FH_ITEM_NAME = "Weymouth Self-Guided Food Tour";
    private const string KEY = "5N8GS1ZZKC8J";

    [HttpPost("newbooking")]
    public async Task<IActionResult> CreateNewBooking([FromBody] JsonElement jsonData, [FromQuery] string key) {
        if (key != KEY) {
            Log.Warning("Received request with invalid key: {Key}", key);
            return Unauthorized("Invalid key provided.");
        }
        try {
            var validationResult = ValidateJsonData(jsonData);
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse());
            }

            var bookingElement = jsonData.GetProperty("booking");
            await ProcessWeymouthTourBooking(bookingElement);

            Log.Information("Successfully processed new booking data.");

            return Ok(CreateSuccessResponse());

        } catch (Exception ex) {
            Log.Error(ex, "Error processing booking JSON packet");
            return CreateErrorResponse("An error occurred whilst processing the booking request.");
        }
    }

    [HttpPost("updatebooking")]
    public async Task<IActionResult> UpdateBooking([FromBody] JsonElement jsonData, [FromQuery] string key) {
        if (key != KEY) {
            Log.Warning("Received request with invalid key: {Key}", key);
            return Unauthorized("Invalid key provided.");
        }
        try {
            var validationResult = ValidateJsonData(jsonData);
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse());
            }

            var bookingElement = jsonData.GetProperty("booking");
            await UpdateFoodTourBooking(bookingElement);

            Log.Information("Successfully received booking update data.");

            return Ok(CreateSuccessResponse());
        } catch (Exception ex) {
            Log.Error(ex, "Error processing booking update JSON packet");
            return CreateErrorResponse("An error occurred whilst processing the booking update request.");
        }
    }

    [HttpPost("cancelbooking")]
    public async Task<IActionResult> CancelBooking([FromBody] JsonElement jsonData, [FromQuery] string key) {
        if (key != KEY) {
            Log.Warning("Received request with invalid key: {Key}", key);
            return Unauthorized("Invalid key provided.");
        }
        try {
            var validationResult = ValidateJsonData(jsonData);
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse());
            }

            var bookingElement = jsonData.GetProperty("booking");

            await CancelFoodTourBooking(bookingElement);

            Log.Information("Successfully received booking cancellation data.");
            return Ok(CreateSuccessResponse());
        } catch (Exception ex) {
            Log.Error(ex, "Error processing booking cancellation JSON packet");
            return CreateErrorResponse("An error occurred whilst processing the booking cancellation request.");
        }
    }


    private readonly Dictionary<string, string> fieldsToLog = new() {
            { "pk", "Booking PK" },
            { "contact.name", "Contact Name" },
            { "contact.email", "Contact Email" },
            { "availability.start_at", "Tour Start Time" },
            { "availability.item.name", "Item Name" },
            { "customer_count", "Customer Count" }
        };

    private record ValidationResult {
        public bool IsValid { get; init; }
        public IList<string> ErrorMessages { get; init; } = [];
    }

    private static ValidationResult ValidateJsonData(JsonElement jsonData) {
        if (jsonData.ValueKind == JsonValueKind.Undefined || jsonData.ValueKind == JsonValueKind.Null) {
            Log.Warning("Received empty or null JSON data");
            return new ValidationResult { IsValid = false };
        }
        return new ValidationResult { IsValid = true };
    }

    private async Task ProcessWeymouthTourBooking(JsonElement bookingElement) {
        if (!IsWeymouthTourBooking(bookingElement)) {
            return;
        }

        var newBooking = new Booking();
        foreach (var field in fieldsToLog) {
            PopulateBookingFromJson(bookingElement, field.Key, field.Value, newBooking);
        }

        // Validate booking data first (excluding purchase code existence check)
        var validationRequest = new BookingValidationRequest {
            TourDate = newBooking.TourDate,
            CustomerEmail = newBooking.CustomerEmail,
            CustomerEmailConfirmation = newBooking.CustomerEmail, // Assuming confirmation is the same as email for API input
            CustomerCount = newBooking.TotalGuests
        };

        var validationResult = await bookingValidationService.ValidateAsync(validationRequest);

        if (!validationResult.IsValid) {
            Log.Error("Booking validation failed for {CustomerEmail}. Errors: {ValidationErrors}",
                newBooking.CustomerEmail,
                string.Join(", ", validationResult.ErrorMessages));
            return;
        }

        // Use database transaction to ensure atomicity
        using var transaction = await context.Database.BeginTransactionAsync();
        try {
            // Check if booking already exists within the transaction to prevent duplicates
            if (!string.IsNullOrEmpty(newBooking.PurchaseCode)) {
                var existingBooking = await context.Bookings
                    .FirstOrDefaultAsync(b => b.PurchaseCode == newBooking.PurchaseCode);

                if (existingBooking != null) {
                    Log.Information("Booking with Purchase Code {PurchaseCode} already exists (ID: {BookingId}). Aborting transaction for duplicate webhook call.",
                        newBooking.PurchaseCode, existingBooking.Id);
                    await transaction.RollbackAsync();
                    return;
                }
            }

            // Purchase code validation within transaction
            var purchaseCodeValidationRequest = new PurchaseCodeValidationRequest {
                PurchaseCode = newBooking.PurchaseCode ?? "",
                ValidationType = PurchaseCodeValidationType.MustNotExist
            };

            var purchaseCodeResult = await purchaseCodeValidationService.ValidateAsync(purchaseCodeValidationRequest);

            if (!purchaseCodeResult.IsValid) {
                Log.Error("Purchase code validation failed for {CustomerEmail}. Error: {ValidationError}",
                    newBooking.CustomerEmail, purchaseCodeResult.ErrorMessage);
                await transaction.RollbackAsync();
                return;
            }

            // Add and save the booking within the transaction
            await context.Bookings.AddAsync(newBooking);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Log.Information("Booking {BookingId}: Saved for {CustomerEmail} on {TourDate:dd/MM/yyyy}.", 
                newBooking.Id, newBooking.CustomerEmail, newBooking.TourDate);

        } catch (Exception ex) {
            Log.Error(ex, "Booking {BookingId}: Failed to save to database. Rolling back transaction.", newBooking.Id);
            await transaction.RollbackAsync();
        }
    }

    private async Task UpdateFoodTourBooking(JsonElement bookingElement) {
        if (!IsWeymouthTourBooking(bookingElement)) {
            return;
        }

        var updatedBooking = new Booking();
        foreach (var field in fieldsToLog) {
            PopulateBookingFromJson(bookingElement, field.Key, field.Value, updatedBooking);
        }

        var validationResults = await ValidateUpdateBooking(updatedBooking);
        if (!validationResults.IsValid) {
            LogValidationErrors(updatedBooking.CustomerEmail, validationResults.ErrorMessages);
            return;
        }

        var existingBooking = await context.Bookings.FirstOrDefaultAsync(b => b.PurchaseCode == updatedBooking.PurchaseCode);
        if (existingBooking == null) {
            Log.Warning("No existing booking found with Purchase Code: {PurchaseCode} for update.", updatedBooking.PurchaseCode);
            return;
        }

        if (!HasBookingChanges(existingBooking, updatedBooking)) {
            Log.Information("Booking {BookingId}: No changes detected for {CustomerEmail}. Skipping update.", existingBooking.Id, existingBooking.CustomerEmail);
            return;
        }

        await UpdateExistingBooking(existingBooking, updatedBooking);
    }

    private async Task CancelFoodTourBooking(JsonElement bookingElement) {
        if (!IsWeymouthTourBooking(bookingElement)) {
            return;
        }

        var bookingToCancel = new Booking();
        foreach (var field in fieldsToLog) {
            PopulateBookingFromJson(bookingElement, field.Key, field.Value, bookingToCancel);
        }

        var validationResult = await ValidateCancellationBooking(bookingToCancel);
        if (!validationResult.IsValid) {
            LogValidationErrors(bookingToCancel.CustomerEmail, validationResult.ErrorMessages);
            return;
        }

        var existingBooking = await context.Bookings.FirstOrDefaultAsync(b => b.PurchaseCode == bookingToCancel.PurchaseCode);
        if (existingBooking == null) {
            Log.Warning("No existing booking found with Purchase Code: {PurchaseCode} for cancellation.", bookingToCancel.PurchaseCode);
            return;
        }

        await CancelExistingBooking(existingBooking);
    }

    private static bool IsWeymouthTourBooking(JsonElement bookingElement) {
        var itemNameElement = NavigateJsonPath(bookingElement, "availability.item.name");
        string itemName = itemNameElement?.ValueKind == JsonValueKind.String
            ? itemNameElement.Value.GetString() ?? string.Empty
            : string.Empty;

        if (itemName != FH_ITEM_NAME) {
            Log.Information("Skipping detailed logging - Item name '{ItemName}' does not match '{FHItem}'", itemName, FH_ITEM_NAME);
            return false;
        }

        Log.Information("Processing booking for {FhItem} - logging detailed values", FH_ITEM_NAME);
        return true;
    }

    private static object CreateSuccessResponse() {
        return new {
            message = "Booking JSON packet received successfully",
            timestamp = DateTime.UtcNow,
        };
    }

    private static object CreateEmptyJsonResponse() {
        return new {
            message = "Empty JSON packet received",
            timestamp = DateTime.UtcNow,
        };
    }

    private ObjectResult CreateErrorResponse(string message) {
        return StatusCode(500, new { message });
    }

    private static void PopulateBookingFromJson(JsonElement rootElement, string jsonPath, string displayName, Booking booking) {
        var element = NavigateJsonPath(rootElement, jsonPath);

        if (!element.HasValue) {
            Log.Information("JSON data {DisplayName}: not found", displayName);
            return;
        }

        switch (displayName) {
            case "Booking PK":
                booking.PurchaseCode = ExtractStringValue(element.Value);
                LogValue(displayName, booking.PurchaseCode);
                break;

            case "Contact Email":
                booking.CustomerEmail = ExtractStringValue(element.Value);
                LogValue(displayName, booking.CustomerEmail);
                break;

            case "Tour Start Time":
                if (TryParseDateTime(element.Value, out var startAt)) {
                    booking.TourDate = startAt;
                    LogValue(displayName, startAt.ToString("O"));
                } else {
                    Log.Information("JSON data {DisplayName}: invalid date format", displayName);
                }
                break;

            case "Customer Count":
                if (TryParseInt(element.Value, out int customerCount)) {
                    booking.TotalGuests = customerCount;
                    LogValue(displayName, customerCount.ToString());
                } else {
                    Log.Information("JSON data {DisplayName}: invalid number format", displayName);
                }
                break;

            case "Contact Name":
            case "Item Name":
                LogValue(displayName, ExtractStringValue(element.Value));
                break;
        }
    }

    private static string ExtractStringValue(JsonElement element) {
        return element.ValueKind switch {
            JsonValueKind.String => element.GetString() ?? "null",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };
    }

    private static bool TryParseDateTime(JsonElement element, out DateTime result) {
        if (element.ValueKind == JsonValueKind.String) {
            return DateTime.TryParse(element.GetString(), out result);
        }
        result = default;
        return false;
    }

    private static bool TryParseInt(JsonElement element, out int result) {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out result)) {
            return true;
        }
        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out result)) {
            return true;
        }
        result = default;
        return false;
    }

    private static void LogValue(string displayName, string value) {
        Log.Debug("JSON data {DisplayName}: {Value}", displayName, value);
    }

    private static JsonElement? NavigateJsonPath(JsonElement rootElement, string path) {
        string[] pathParts = path.Split('.');
        var currentElement = rootElement;

        foreach (string part in pathParts) {
            if (currentElement.TryGetProperty(part, out var nextElement)) {
                currentElement = nextElement;
            } else {
                return null;
            }
        }

        return currentElement;
    }

    private static bool HasBookingChanges(Booking existingBooking, Booking updatedBooking) {
        return existingBooking.TourDate != updatedBooking.TourDate ||
               existingBooking.CustomerEmail != updatedBooking.CustomerEmail ||
               existingBooking.TotalGuests != updatedBooking.TotalGuests;
    }

    private async Task<ValidationResult> ValidateUpdateBooking(Booking booking) {
        var purchaseCodeValidationRequest = new PurchaseCodeValidationRequest {
            PurchaseCode = booking.PurchaseCode ?? string.Empty,
            ValidationType = PurchaseCodeValidationType.MustExist
        };

        var purchaseCodeResult = await purchaseCodeValidationService.ValidateAsync(purchaseCodeValidationRequest);

        var validationRequest = new BookingValidationRequest {
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            CustomerEmailConfirmation = booking.CustomerEmail,
            CustomerCount = booking.TotalGuests
        };

        var validationResult = await bookingValidationService.ValidateAsync(validationRequest);

        var allErrors = new List<string>();
        allErrors.AddRange(validationResult.ErrorMessages);
        if (!purchaseCodeResult.IsValid && purchaseCodeResult.ErrorMessage != null) {
            allErrors.Add(purchaseCodeResult.ErrorMessage);
        }

        return new ValidationResult {
            IsValid = purchaseCodeResult.IsValid && validationResult.IsValid,
            ErrorMessages = allErrors
        };
    }

    private async Task UpdateExistingBooking(Booking existingBooking, Booking updatedBooking) {
        existingBooking.TourDate = updatedBooking.TourDate;
        existingBooking.CustomerEmail = updatedBooking.CustomerEmail;
        existingBooking.TotalGuests = updatedBooking.TotalGuests;

        try {
            context.Bookings.Update(existingBooking);
            await context.SaveChangesAsync();
            Log.Information("Booking {BookingId}: Updated for {CustomerEmail} on {TourDate:dd/MM/yyyy}.",
                existingBooking.Id, existingBooking.CustomerEmail, existingBooking.TourDate);
        } catch (Exception ex) {
            Log.Error(ex, "Booking {BookingId}: Failed to update in database.", existingBooking.Id);
        }
    }

    private static void LogValidationErrors(string customerEmail, IList<string> errorMessages) {
        Log.Warning("Booking update validation failed for {CustomerEmail}. Errors: {ValidationErrors}",
            customerEmail, string.Join(", ", errorMessages));
    }

    private async Task<ValidationResult> ValidateCancellationBooking(Booking booking) {
        var purchaseCodeValidationRequest = new PurchaseCodeValidationRequest {
            PurchaseCode = booking.PurchaseCode ?? string.Empty,
            ValidationType = PurchaseCodeValidationType.MustExist
        };

        var purchaseCodeResult = await purchaseCodeValidationService.ValidateAsync(purchaseCodeValidationRequest);

        var errorMessages = new List<string>();
        if (!purchaseCodeResult.IsValid && purchaseCodeResult.ErrorMessage != null) {
            errorMessages.Add(purchaseCodeResult.ErrorMessage);
        }

        return new ValidationResult {
            IsValid = purchaseCodeResult.IsValid,
            ErrorMessages = errorMessages
        };
    }

    private async Task CancelExistingBooking(Booking existingBooking) {
        existingBooking.IsCancelled = true;
        context.Bookings.Update(existingBooking);

        Log.Information("Booking {BookingId}: Marked as cancelled for {CustomerEmail} on {TourDate:dd/MM/yyyy}.",
            existingBooking.Id, existingBooking.CustomerEmail, existingBooking.TourDate);

        try {
            await context.SaveChangesAsync();
        } catch (Exception ex) {
            Log.Error(ex, "Purchase code:{PurchaseCode} Failed to update in database.", existingBooking.PurchaseCode);
        }
    }
}