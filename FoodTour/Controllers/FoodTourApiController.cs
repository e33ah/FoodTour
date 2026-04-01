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
            var validationResult = ValidateAndLogJsonData(jsonData, "booking");
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse());
            }
            // Log the complete JSON packet contents temporarily to test
            string jsonString = jsonData.GetRawText();
            Log.Information("Received new booking cancellation JSON packet at {Date}. Data: {JsonData}", DateTime.UtcNow, jsonString);

            var bookingElement = UnwrapJsonData(jsonData, "booking");
            await ProcessWeymouthTourBooking(bookingElement);

            int propertyCount = CountJsonProperties(jsonData);
            Log.Information("Successfully processed booking data with {PropertyCount} root properties", propertyCount);

            return Ok(CreateSuccessResponse(propertyCount));

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
            var validationResult = ValidateAndLogJsonData(jsonData, "booking update");
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse());
            }

            // Log the complete JSON packet contents temporarily to test
            string jsonString = jsonData.GetRawText();
            Log.Information("Received new booking JSON packet at {Date}. Data: {JsonData}", DateTime.UtcNow, jsonString);

            var bookingElement = UnwrapJsonData(jsonData, "booking");
            await UpdateFoodTourBooking(bookingElement);

            int propertyCount = CountJsonProperties(jsonData);
            Log.Information("Successfully received booking update data with {PropertyCount} root properties", propertyCount);

            return Ok(CreateSuccessResponse(propertyCount));
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
            var validationResult = ValidateAndLogJsonData(jsonData, "booking cancellation");
            if (!validationResult.IsValid) {
                return Ok(CreateEmptyJsonResponse());
            }
            // Log the complete JSON packet contents temporarily to test
            string jsonString = jsonData.GetRawText();
            Log.Information("Received new booking cancellation JSON packet at {Date}. Data: {JsonData}", DateTime.UtcNow, jsonString);

            var bookingElement = UnwrapJsonData(jsonData, "booking");

            await CancelFoodTourBooking(bookingElement);

            int propertyCount = CountJsonProperties(jsonData);
            Log.Information("Successfully received booking cancellation data with {PropertyCount} root properties", propertyCount);
            return Ok(CreateSuccessResponse(propertyCount));
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

    private static JsonValidationResult ValidateAndLogJsonData(JsonElement jsonData, string dataType) {
        if (jsonData.ValueKind == JsonValueKind.Undefined || jsonData.ValueKind == JsonValueKind.Null) {
            Log.Warning("Received empty or null JSON data");
            return new JsonValidationResult { IsValid = false };
        }

        string jsonString = jsonData.GetRawText();
        Log.Information("Received new {DataType} JSON packet at {Timestamp}. Raw data length: {DataLength} characters",
            dataType, DateTime.UtcNow, jsonString.Length);

        return new JsonValidationResult { IsValid = true, DataLength = jsonString.Length };
    }

    private static JsonElement UnwrapJsonData(JsonElement jsonData, string wrapperProperty) {
        if (jsonData.TryGetProperty(wrapperProperty, out var wrappedData)) {
            Log.Information("Detected wrapped {WrapperProperty} format", wrapperProperty);
            return wrappedData;
        }

        Log.Information("Detected direct {WrapperProperty} format", wrapperProperty);
        return jsonData;
    }

    private async Task ProcessWeymouthTourBooking(JsonElement bookingElement) {
        if (!IsWeymouthTourBooking(bookingElement)) {
            return;
        }

        var newBooking = new Booking();
        foreach (var field in fieldsToLog) {
            PopulateBookingFromJson(bookingElement, field.Key, field.Value, newBooking);
        }

        var purchaseCodeValidationRequest = new PurchaseCodeValidationRequest {
            PurchaseCode = newBooking.PurchaseCode ?? "",
            ValidationType = PurchaseCodeValidationType.MustNotExist
        };

        var purchaseCodeResult = await purchaseCodeValidationService.ValidateAsync(purchaseCodeValidationRequest);

        // Validate other booking data (excluding purchase code)
        var validationRequest = new BookingValidationRequest {
            TourDate = newBooking.TourDate,
            CustomerEmail = newBooking.CustomerEmail,
            CustomerEmailConfirmation = newBooking.CustomerEmail, // Assuming confirmation is the same as email for API input
            CustomerCount = newBooking.TotalGuests
        };

        var validationResult = await bookingValidationService.ValidateAsync(validationRequest);

        if (validationResult.IsValid && purchaseCodeResult.IsValid) {
            try {
                await context.Bookings.AddAsync(newBooking);
                await context.SaveChangesAsync();

                Log.Information("Booking {BookingId}: Saved for {CustomerEmail} on {TourDate:dd/MM/yyyy}.", newBooking.Id, newBooking.CustomerEmail, newBooking.TourDate);
            } catch (Exception ex) {
                Log.Error(ex, "Booking {BookingId}: Failed to save to database.", newBooking.Id);
            }
        } else {
            var allErrors = new List<string>();
            allErrors.AddRange(validationResult.ErrorMessages);
            if (!purchaseCodeResult.IsValid && purchaseCodeResult.ErrorMessage != null) {
                allErrors.Add(purchaseCodeResult.ErrorMessage);
            }

            Log.Error("Booking validation failed for {CustomerEmail}. Errors: {ValidationErrors}",
                newBooking.CustomerEmail,
                string.Join(", ", allErrors));
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

        var purchaseCodeValidationRequest = new PurchaseCodeValidationRequest {
            PurchaseCode = updatedBooking.PurchaseCode ?? string.Empty,
            ValidationType = PurchaseCodeValidationType.MustExist
        };

        var purchaseCodeResult = await purchaseCodeValidationService.ValidateAsync(purchaseCodeValidationRequest);

        // Validate other booking data (excluding purchase code)
        var validationRequest = new BookingValidationRequest {
            TourDate = updatedBooking.TourDate,
            CustomerEmail = updatedBooking.CustomerEmail,
            CustomerEmailConfirmation = updatedBooking.CustomerEmail, // Assuming confirmation is the same as email for API input
            CustomerCount = updatedBooking.TotalGuests
        };

        var validationResult = await bookingValidationService.ValidateAsync(validationRequest);

        if (purchaseCodeResult.IsValid && validationResult.IsValid) {
            var existingBooking = await context.Bookings.FirstOrDefaultAsync(b => b.PurchaseCode == updatedBooking.PurchaseCode);
            if (existingBooking != null) {
                existingBooking.TourDate = updatedBooking.TourDate;
                existingBooking.CustomerEmail = updatedBooking.CustomerEmail;
                existingBooking.TotalGuests = updatedBooking.TotalGuests;
                try {
                    context.Bookings.Update(existingBooking);
                    await context.SaveChangesAsync();
                    Log.Information("Booking {BookingId}: Updated for {CustomerEmail} on {TourDate:dd/MM/yyyy}.", existingBooking.Id, existingBooking.CustomerEmail, existingBooking.TourDate);
                } catch (Exception ex) {
                    Log.Error(ex, "Booking {BookingId}: Failed to update in database.", existingBooking.Id);
                }
            } else {
                Log.Warning("No existing booking found with Purchase Code: {PurchaseCode} for update.", updatedBooking.PurchaseCode);
            }
        } else {
            var allErrors = new List<string>();
            allErrors.AddRange(validationResult.ErrorMessages);
            if (!purchaseCodeResult.IsValid && purchaseCodeResult.ErrorMessage != null) {
                allErrors.Add(purchaseCodeResult.ErrorMessage);
            }
            Log.Warning("Booking update validation failed for {CustomerEmail}. Errors: {ValidationErrors}",
                updatedBooking.CustomerEmail, string.Join(", ", allErrors));
        }
    }

    private async Task CancelFoodTourBooking(JsonElement bookingElement) {
        if (!IsWeymouthTourBooking(bookingElement)) {
            return;
        }
        var updatedBooking = new Booking();
        foreach (var field in fieldsToLog) {
            PopulateBookingFromJson(bookingElement, field.Key, field.Value, updatedBooking);
        }
        var purchaseCodeValidationRequest = new PurchaseCodeValidationRequest {
            PurchaseCode = updatedBooking.PurchaseCode ?? string.Empty,
            ValidationType = PurchaseCodeValidationType.MustExist
        };
        var purchaseCodeResult = await purchaseCodeValidationService.ValidateAsync(purchaseCodeValidationRequest);
        if (purchaseCodeResult.IsValid) {
            var existingBooking = await context.Bookings.FirstOrDefaultAsync(b => b.PurchaseCode == updatedBooking.PurchaseCode);
            if (existingBooking != null) {
                existingBooking.IsCancelled = true;
                context.Bookings.Update(existingBooking);
                Log.Information("Booking {BookingId}: Marked as cancelled for {CustomerEmail} on {TourDate:dd/MM/yyyy}.", existingBooking.Id, existingBooking.CustomerEmail, existingBooking.TourDate);
            } else {
                Log.Warning("No existing booking found with Purchase Code: {PurchaseCode} for update.", updatedBooking.PurchaseCode);
            }
            try {
                await context.SaveChangesAsync();
            } catch (Exception ex) {
                Log.Error(ex, "Purchase code:{PurchaseCode} Failed to update in database.", updatedBooking.PurchaseCode);
            }
        } else {
            string errorMessage = purchaseCodeResult.ErrorMessage ?? "Unknown error during purchase code validation.";
            Log.Warning("Purchase code validation failed for {CustomerEmail}. Error: {ErrorMessage}", updatedBooking.CustomerEmail, errorMessage);
        }
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

    private static int CountJsonProperties(JsonElement jsonData) {
        int propertyCount = 0;
        foreach (var property in jsonData.EnumerateObject()) {
            propertyCount++;
        }
        return propertyCount;
    }

    private static object CreateSuccessResponse(int propertyCount) {
        return new {
            message = "Booking JSON packet received successfully",
            timestamp = DateTime.UtcNow,
            receivedProperties = propertyCount,
        };
    }

    private static object CreateEmptyJsonResponse() {
        return new {
            message = "Empty JSON packet received",
            timestamp = DateTime.UtcNow,
            receivedProperties = 0
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
        Log.Information("JSON data {DisplayName}: {Value}", displayName, value);
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
}