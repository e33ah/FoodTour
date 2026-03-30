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

            // Log the complete JSON packet contents
            string jsonString = jsonData.GetRawText();
            Log.Information("Received new booking JSON packet at {Date}. Data: {JsonData}", DateTime.UtcNow, jsonString);

            // Log individual properties if they exist
            if (jsonData.TryGetProperty("customerEmail", out var customerEmailElement)) {
                Log.Information("Customer Email: {Email}", customerEmailElement.GetString());
            }

            if (jsonData.TryGetProperty("availability", out var tourDateElement)) {
                Log.Information("Tour Date: {TourDate}", tourDateElement.GetString());
            }

            if (jsonData.TryGetProperty("uuid", out var purchaseCodeElement)) {
                Log.Information("Purchase Code: {PurchaseCode}", purchaseCodeElement.GetString());
            }
            if (jsonData.TryGetProperty("customer_count", out var customerCountElement)) {
                Log.Information("Customer Count: {PurchaseCode}", customerCountElement.GetString());
            }

            // Log all properties in the JSON
            int propertyCount = 0;
            foreach (var property in jsonData.EnumerateObject()) {
                Log.Information("JSON Property: {PropertyName} = {PropertyValue}", 
                    property.Name, property.Value.GetRawText());
                propertyCount++;
            }

            // Return success response for now
            return Ok(new { 
                message = "JSON packet received and logged successfully",
                timestamp = DateTime.UtcNow,
                receivedProperties = propertyCount
            });

        } catch (Exception ex) {
            Log.Error(ex, "Error processing JSON packet");
            return StatusCode(500, new { message = "An error occurred whilst processing the request." });
        }
    }
}