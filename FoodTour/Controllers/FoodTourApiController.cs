namespace FoodTour.Controllers;

[ApiController]
[Route("api/foodtour")]
public class FoodTourApiController(FoodTourContext context) : ControllerBase {

    [HttpPost("newbooking")]
    public async Task<IActionResult> CreateNewBooking([FromBody] JsonElement jsonData) {
        try {
            // Check if we have valid JSON data
            if (jsonData.ValueKind == JsonValueKind.Undefined || jsonData.ValueKind == JsonValueKind.Null) {
                Log.Warning("Received empty or null JSON data");
                return Ok(new { 
                    message = "Empty JSON packet received",
                    timestamp = DateTime.UtcNow,
                    receivedProperties = 0
                });
            }

            // Log the complete JSON packet size
            string jsonString = jsonData.GetRawText();
            Log.Information("Received new booking JSON packet at {Timestamp}. Raw data length: {DataLength} characters", 
                DateTime.UtcNow, jsonString.Length);

            // Determine if data is wrapped in 'booking' object or at root level
            var bookingElement = jsonData;
            if (jsonData.TryGetProperty("booking", out var wrappedBooking)) {
                bookingElement = wrappedBooking;
                Log.Information("Detected wrapped booking format");
            } else {
                Log.Information("Detected direct booking format");
            }

            // Extract and log specific values only if this is for the Weymouth Self-Guided Food Tour
            var itemNameElement = NavigateJsonPath(bookingElement, "availability.item.name");
            string itemName = itemNameElement?.ValueKind == JsonValueKind.String 
                ? itemNameElement.Value.GetString() ?? "" 
                : "";

            if (itemName == "Weymouth Self-Guided Food Tour") {
                Log.Information("Processing booking for Weymouth Self-Guided Food Tour - logging detailed values");

                ExtractAndLogValue(bookingElement, "pk", "Booking PK");
                ExtractAndLogValue(bookingElement, "contact.name", "Contact Name");
                ExtractAndLogValue(bookingElement, "contact.email", "Contact Email");
                ExtractAndLogValue(bookingElement, "availability.start_at", "Tour Start Time");
                ExtractAndLogValue(bookingElement, "availability.item.name", "Item Name");
                ExtractAndLogValue(bookingElement, "customer_count", "Customer Count");
            } else {
                Log.Information("Skipping detailed logging - Item name '{ItemName}' does not match 'Weymouth Self-Guided Food Tour'", itemName);
            }

            // Count total properties for response
            int propertyCount = 0;
            foreach (var property in jsonData.EnumerateObject()) {
                propertyCount++;
            }

            Log.Information("Successfully processed booking data with {PropertyCount} root properties", propertyCount);

            // Return success response
            return Ok(new { 
                message = "Booking JSON packet received successfully",
                timestamp = DateTime.UtcNow,
                receivedProperties = propertyCount,
            });

        } catch (Exception ex) {
            Log.Error(ex, "Error processing booking JSON packet");
            return StatusCode(500, new { message = "An error occurred whilst processing the booking request." });
        }
    }

    private static void ExtractAndLogValue(JsonElement rootElement, string jsonPath, string displayName) {
        var element = NavigateJsonPath(rootElement, jsonPath);

        if (element.HasValue) {
            string value = element.Value.ValueKind switch {
                JsonValueKind.String => element.Value.GetString() ?? "null",
                JsonValueKind.Number => element.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                _ => element.Value.GetRawText()
            };
            Log.Information("JSON data {DisplayName}: {Value}", displayName, value);
        } else {
            Log.Information("JSON data {DisplayName}: not found", displayName);
        }
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