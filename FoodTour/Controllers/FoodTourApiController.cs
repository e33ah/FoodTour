namespace FoodTour.Controllers;

[ApiController]
[Route("api/foodtour")]
public partial class FoodTourApiController(FoodTourContext context, IBookingValidationService bookingValidationService, IPurchaseCodeValidationService purchaseCodeValidationService) : ControllerBase {
    private const string FH_ITEM_NAME_KEY = "availability.item.name";
    private const string FH_ITEM_NAME_VALUE = "Weymouth Self-Guided Food Tour";
    private const string KEY = "5N8GS1ZZKC8J";

    [HttpPost("fhwebhook/newbooking")]
    public async Task<IActionResult> CreateNewBooking([FromBody] JsonElement jsonData, [FromQuery] string key) {
        if (key != KEY) {
            Log.Warning("Received request with invalid key: {Key}", key);
            return Unauthorized("Invalid key provided.");
        }
        try {
            var validationResult = ValidateJsonData(jsonData);
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse()); // Return success to prevent webhook retries, even if there was an error processing the data
            }

            var bookingElement = jsonData.GetProperty("booking");
            if (IsWeymouthTourBooking(bookingElement)) {
                await ProcessNewFoodTourBooking(bookingElement);
                Log.Information("Successfully processed new booking data.");
            }

            return Ok(CreateSuccessResponse());

        } catch (Exception ex) {
            Log.Error(ex, "Error processing booking JSON packet");
            return Ok(CreateSuccessResponse()); // Return success to prevent webhook retries, even if there was an error processing the data
        }
    }

    [HttpPost("fhwebhook/updatebooking")]
    public async Task<IActionResult> UpdateBooking([FromBody] JsonElement jsonData, [FromQuery] string key) {
        if (key != KEY) {
            Log.Warning("Received request with invalid key: {Key}", key);
            return Unauthorized("Invalid key provided.");
        }
        try {
            var validationResult = ValidateJsonData(jsonData);
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse()); // Return success to prevent webhook retries, even if there was an error processing the data
            }

            var bookingElement = jsonData.GetProperty("booking");
            if (IsWeymouthTourBooking(bookingElement)) {
                await ProcessUpdateFoodTourBooking(bookingElement);
                Log.Information("Successfully received booking update data.");
            }

            return Ok(CreateSuccessResponse());

        } catch (Exception ex) {
            Log.Error(ex, "Error processing booking update JSON packet");
            return Ok(CreateSuccessResponse()); // Return success to prevent webhook retries, even if there was an error processing the data
        }
    }

    [HttpPost("fhwebhook/cancelbooking")]
    public async Task<IActionResult> CancelBooking([FromBody] JsonElement jsonData, [FromQuery] string key) {
        if (key != KEY) {
            Log.Warning("Received request with invalid key: {Key}", key);
            return Unauthorized("Invalid key provided.");
        }
        try {
            var validationResult = ValidateJsonData(jsonData);
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse()); // Return success to prevent webhook retries, even if there was an error processing the data
            }

            var bookingElement = jsonData.GetProperty("booking");
            if (IsWeymouthTourBooking(bookingElement)) {
                await ProcessCancelFoodTourBooking(bookingElement);
                Log.Information("Successfully received booking cancellation data.");
            }

            return Ok(CreateSuccessResponse());

        } catch (Exception ex) {
            Log.Error(ex, "Error processing booking cancellation JSON packet");
            return Ok(CreateSuccessResponse()); // Return success to prevent webhook retries, even if there was an error processing the data
        }
    }

    [HttpPost("checkbooking")]
    public async Task<IActionResult> CheckBooking([FromBody] BookingCheckRequest request) {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || string.IsNullOrWhiteSpace(request.PurchaseCode)) {
            return BadRequest(new { found = false, message = "Email and booking reference are required." });
        }

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.CustomerEmail.ToLower() == request.CustomerEmail.ToLower() && b.PurchaseCode == request.PurchaseCode);

        if (booking == null) {
            Log.Debug("No booking found for email {Email} and purchase code {PurchaseCode}", request.CustomerEmail, request.PurchaseCode);
            return Ok(new { found = false, message = "We couldn't find your booking. Please check your booking reference and email address for any typos, or contact support if the problem continues" });
        }

        if (booking.IsCancelled) {
            Log.Information("Booking {BookingId}: Found but cancelled for {CustomerEmail} with purchase code {PurchaseCode}", booking.Id, booking.CustomerEmail, booking.PurchaseCode);
            return Ok(new { found = true, isCancelled = true, message = "This booking has been cancelled. Please contact support if you believe this is an error.", bookingId = booking.Id });
        }

        Log.Information("Booking {BookingId}: Found for {CustomerEmail} with purchase code {PurchaseCode}", booking.Id, booking.CustomerEmail, booking.PurchaseCode);
        return Ok(new { found = true, isCancelled = false, message = "We've found your booking!", bookingId = booking.Id });
    }


    private static object CreateSuccessResponse() => new { message = "Booking JSON packet received successfully", timestamp = DateTime.UtcNow };

    private static object CreateEmptyJsonResponse() => new { message = "Empty JSON packet received", timestamp = DateTime.UtcNow };

    private readonly Dictionary<string, string> fieldsToLog = new() {
            { "pk", "Booking PK" },
            { "contact.name", "Contact Name" },
            { "contact.email", "Contact Email" },
            { "availability.start_at", "Tour Start Time" },
            { FH_ITEM_NAME_KEY, "Item Name" },
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
    private static bool IsWeymouthTourBooking(JsonElement bookingElement) {
        var itemNameElement = NavigateJsonPath(bookingElement, FH_ITEM_NAME_KEY);
        string itemName = itemNameElement?.ValueKind == JsonValueKind.String
            ? itemNameElement.Value.GetString() ?? string.Empty
            : string.Empty;

        if (itemName != FH_ITEM_NAME_VALUE) {
            Log.Debug("Skipping detailed logging - Item name '{ItemName}' does not match '{FHItem}'", itemName, FH_ITEM_NAME_VALUE);
            return false;
        }

        Log.Information("Processing booking for {FhItem}", FH_ITEM_NAME_VALUE);
        return true;
    }

    private async Task ProcessNewFoodTourBooking(JsonElement bookingElement) {
        var newBooking = new Booking();
        foreach (var field in fieldsToLog) {
            PopulateBookingFromJson(bookingElement, field.Key, field.Value, newBooking);
        }

        var validationResults = await ValidateNewBooking(newBooking);
        if (!validationResults.IsValid) {
            LogValidationErrors(newBooking.CustomerEmail, validationResults.ErrorMessages);
            return;
        }

        await SaveNewBookingWithTransaction(newBooking);
    }

    private async Task ProcessUpdateFoodTourBooking(JsonElement bookingElement) {
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

    private static bool HasBookingChanges(Booking existingBooking, Booking updatedBooking) {
        return existingBooking.TourDate != updatedBooking.TourDate ||
               existingBooking.CustomerEmail != updatedBooking.CustomerEmail ||
               existingBooking.TotalGuests != updatedBooking.TotalGuests;
    }

    private async Task ProcessCancelFoodTourBooking(JsonElement bookingElement) {
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

            case "Contact Name":
               booking.CustomerName = ExtractStringValue(element.Value);
                LogValue(displayName, booking.CustomerName);
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

            case "Item Name":
                LogValue(displayName, ExtractStringValue(element.Value));
                break;
        }
    }
    private static void LogValue(string displayName, string value) => Log.Debug("JSON data {DisplayName}: {Value}", displayName, value);


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


    private async Task<ValidationResult> ValidateNewBooking(Booking booking) {
        var validationRequest = new BookingValidationRequest {
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            CustomerCount = booking.TotalGuests,
            CustomerName = booking.CustomerName
        };

        var validationResult = await bookingValidationService.ValidateAsync(validationRequest);

        var purchaseCodeValidationRequest = new PurchaseCodeValidationRequest {
            PurchaseCode = booking.PurchaseCode ?? "",
            ValidationType = PurchaseCodeValidationType.MustNotExist
        };

        var purchaseCodeResult = await purchaseCodeValidationService.ValidateAsync(purchaseCodeValidationRequest);

        var allErrors = new List<string>();
        allErrors.AddRange(validationResult.ErrorMessages);
        if (!purchaseCodeResult.IsValid && purchaseCodeResult.ErrorMessage != null) {
            allErrors.Add(purchaseCodeResult.ErrorMessage);
        }

        return new ValidationResult {
            IsValid = validationResult.IsValid && purchaseCodeResult.IsValid,
            ErrorMessages = allErrors
        };
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
    private static void LogValidationErrors(string customerEmail, IList<string> errorMessages) {
        Log.Warning("Booking validation failed for {CustomerEmail}. Errors: {ValidationErrors}",
            customerEmail, string.Join(", ", errorMessages));
    }

    private async Task SaveNewBookingWithTransaction(Booking newBooking) {
        using var transaction = await context.Database.BeginTransactionAsync();
        try {
            // Check if booking already exists within the transaction to prevent duplicates from race conditions
            if (!string.IsNullOrEmpty(newBooking.PurchaseCode)) {
                var existingBooking = await context.Bookings
                    .FirstOrDefaultAsync(b => b.PurchaseCode == newBooking.PurchaseCode);

                if (existingBooking != null) {
                    Log.Debug("Booking with Purchase Code {PurchaseCode} already exists (ID: {BookingId}). Aborting transaction for duplicate webhook call.",
                        newBooking.PurchaseCode, existingBooking.Id);
                    await transaction.RollbackAsync();
                    return;
                }
            }

            await context.Bookings.AddAsync(newBooking);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Log.Information("Booking {BookingId}: Saved for {CustomerEmail} on {TourDate:dd/MM/yyyy}.", newBooking.Id, newBooking.CustomerEmail, newBooking.TourDate);

        } catch (Exception ex) {
            Log.Error(ex, "Booking {BookingId}: Failed to save to database. Rolling back transaction.", newBooking.Id);
            await transaction.RollbackAsync();
        }
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

    private static string ExtractStringValue(JsonElement element) => element.ValueKind switch {
        JsonValueKind.String => element.GetString() ?? "null",
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => element.GetRawText()
    };

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
}