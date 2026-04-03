namespace FoodTour.Test.Controllers;

public class FoodTourApiControllerHelperMethodsTests {
    [Theory]
    [InlineData("2025-06-15T10:00:00Z", "original@test.com", "Original Name", 2,
                "2025-06-20T10:00:00Z", "original@test.com", "Original Name", 2, true)] // Tour date changed
    [InlineData("2025-06-15T10:00:00Z", "original@test.com", "Original Name", 2,
                "2025-06-15T10:00:00Z", "updated@test.com", "Original Name", 2, true)] // Email changed
    [InlineData("2025-06-15T10:00:00Z", "original@test.com", "Original Name", 2,
                "2025-06-15T10:00:00Z", "original@test.com", "Updated Name", 2, true)] // Name changed
    [InlineData("2025-06-15T10:00:00Z", "original@test.com", "Original Name", 2,
                "2025-06-15T10:00:00Z", "original@test.com", "Original Name", 4, true)] // Guest count changed
    [InlineData("2025-06-15T10:00:00Z", "original@test.com", "Original Name", 2,
                "2025-06-15T10:00:00Z", "original@test.com", "Original Name", 2, false)] // No changes
    public void HasBookingChanges_DetectsChanges_Correctly(
        string originalDateStr, string originalEmail, string originalName, int originalGuests,
        string updatedDateStr, string updatedEmail, string updatedName, int updatedGuests,
        bool expectedHasChanges) {
        var existingBooking = new Booking {
            TourDate = DateTime.Parse(originalDateStr),
            CustomerEmail = originalEmail,
            CustomerName = originalName,
            TotalGuests = originalGuests
        };

        var updatedBooking = new Booking {
            TourDate = DateTime.Parse(updatedDateStr),
            CustomerEmail = updatedEmail,
            CustomerName = updatedName,
            TotalGuests = updatedGuests
        };

        var hasChanges = HasBookingChangesPublic(existingBooking, updatedBooking);

        hasChanges.ShouldBe(expectedHasChanges);
    }

    [Fact]
    public void HasBookingChanges_ReturnsTrue_WhenAllFieldsChanged() {
        var existingBooking = new Booking {
            TourDate = DateTime.Parse("2025-06-15T10:00:00Z"),
            CustomerEmail = "original@test.com",
            CustomerName = "Original Name",
            TotalGuests = 2
        };

        var updatedBooking = new Booking {
            TourDate = DateTime.Parse("2025-06-20T10:00:00Z"),
            CustomerEmail = "updated@test.com",
            CustomerName = "Updated Name",
            TotalGuests = 4
        };

        var hasChanges = HasBookingChangesPublic(existingBooking, updatedBooking);

        hasChanges.ShouldBeTrue();
    }

    [Fact]
    public void HasBookingChanges_ReturnsFalse_WhenNoFieldsChanged() {
        var existingBooking = new Booking {
            TourDate = DateTime.Parse("2025-06-15T10:00:00Z"),
            CustomerEmail = "same@test.com",
            CustomerName = "Same Name",
            TotalGuests = 2
        };

        var updatedBooking = new Booking {
            TourDate = DateTime.Parse("2025-06-15T10:00:00Z"),
            CustomerEmail = "same@test.com",
            CustomerName = "Same Name",
            TotalGuests = 2
        };

        var hasChanges = HasBookingChangesPublic(existingBooking, updatedBooking);

        hasChanges.ShouldBeFalse();
    }

    [Theory]
    [InlineData("contact.email", "test@example.com")]
    [InlineData("contact.name", "Test User")]
    [InlineData("availability.start_at", "2025-06-15T10:00:00Z")]
    [InlineData("availability.item.name", "Weymouth Self-Guided Food Tour")]
    [InlineData("customer_count", "2")]
    [InlineData("pk", "TEST123")]
    public void NavigateJsonPath_ReturnsElement_WhenPathExists(string jsonPath, string expectedValue) {
        string jsonString = """
            {
                "contact": {
                    "email": "test@example.com",
                    "name": "Test User"
                },
                "availability": {
                    "start_at": "2025-06-15T10:00:00Z",
                    "item": {
                        "name": "Weymouth Self-Guided Food Tour"
                    }
                },
                "customer_count": 2,
                "pk": "TEST123"
            }
            """;
        var rootElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = NavigateJsonPathPublic(rootElement, jsonPath);

        result.ShouldNotBeNull();
        var actualValue = result.Value.ValueKind == JsonValueKind.String 
            ? result.Value.GetString() 
            : result.Value.GetRawText();
        actualValue.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("nonexistent.path")]
    [InlineData("contact.nonexistent")]
    [InlineData("availability.item.nonexistent")]
    public void NavigateJsonPath_ReturnsNull_WhenPathDoesNotExist(string jsonPath) {
        string jsonString = """
            {
                "contact": {
                    "email": "test@example.com"
                },
                "availability": {
                    "item": {
                        "name": "Test"
                    }
                }
            }
            """;
        var rootElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = NavigateJsonPathPublic(rootElement, jsonPath);

        result.ShouldBeNull();
    }

    [Fact]
    public void NavigateJsonPath_HandlesSingleLevelPath() {
        string jsonString = """
            {
                "pk": "TEST123"
            }
            """;
        var rootElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = NavigateJsonPathPublic(rootElement, "pk");

        result.ShouldNotBeNull();
        result.Value.GetString().ShouldBe("TEST123");
    }

    [Fact]
    public void NavigateJsonPath_HandlesDeepNestedPath() {
        string jsonString = """
            {
                "level1": {
                    "level2": {
                        "level3": {
                            "level4": {
                                "value": "deep"
                            }
                        }
                    }
                }
            }
            """;
        var rootElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = NavigateJsonPathPublic(rootElement, "level1.level2.level3.level4.value");

        result.ShouldNotBeNull();
        result.Value.GetString().ShouldBe("deep");
    }

    [Theory]
    [InlineData("\"test\"", "test")]
    [InlineData("123", "123")]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("null", "null")]
    public void ExtractStringValue_HandlesVariousValueKinds(string jsonValue, string expectedResult) {
        string jsonString = $"{{\"value\": {jsonValue}}}";
        var rootElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var element = rootElement.GetProperty("value");

        var result = ExtractStringValuePublic(element);

        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData("\"2025-06-15T10:00:00Z\"", true)]
    [InlineData("\"2025-01-01\"", true)]
    [InlineData("\"invalid-date\"", false)]
    [InlineData("123", false)]
    [InlineData("null", false)]
    public void TryParseDateTime_HandlesVariousFormats(string jsonValue, bool expectedSuccess) {
        string jsonString = $"{{\"value\": {jsonValue}}}";
        var rootElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var element = rootElement.GetProperty("value");

        var success = TryParseDateTimePublic(element, out var result);

        success.ShouldBe(expectedSuccess);
        if (expectedSuccess) {
            result.ShouldBeOfType<DateTime>();
        }
    }

    [Theory]
    [InlineData("42", true, 42)]
    [InlineData("\"42\"", true, 42)]
    [InlineData("0", true, 0)]
    [InlineData("\"-1\"", true, -1)]
    [InlineData("\"invalid\"", false, 0)]
    [InlineData("null", false, 0)]
    [InlineData("true", false, 0)]
    public void TryParseInt_HandlesVariousFormats(string jsonValue, bool expectedSuccess, int expectedValue) {
        string jsonString = $"{{\"value\": {jsonValue}}}";
        var rootElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var element = rootElement.GetProperty("value");

        var success = TryParseIntPublic(element, out var result);

        success.ShouldBe(expectedSuccess);
        result.ShouldBe(expectedValue);
    }

    [Fact]
    public void ValidateJsonData_ReturnsValid_WhenJsonIsValid() {
        string jsonString = """
            {
                "booking": {
                    "pk": "TEST123"
                }
            }
            """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = ValidateJsonDataPublic(jsonElement);

        result.GetType().GetProperty("IsValid")!.GetValue(result).ShouldBe(true);
    }

    [Fact]
    public void ValidateJsonData_ReturnsInvalid_WhenJsonIsNull() {
        var jsonElement = new JsonElement();

        var result = ValidateJsonDataPublic(jsonElement);

        result.GetType().GetProperty("IsValid")!.GetValue(result).ShouldBe(false);
    }

    [Fact]
    public void IsWeymouthTourBooking_ReturnsTrue_WhenItemNameMatches() {
        string jsonString = """
            {
                "availability": {
                    "item": {
                        "name": "Weymouth Self-Guided Food Tour"
                    }
                }
            }
            """;
        var bookingElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = IsWeymouthTourBookingPublic(bookingElement);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Different Tour")]
    [InlineData("")]
    [InlineData("Weymouth Tour")] // Partial match should fail
    public void IsWeymouthTourBooking_ReturnsFalse_WhenItemNameDoesNotMatch(string itemName) {
        string jsonString = $$"""
            {
                "availability": {
                    "item": {
                        "name": "{{itemName}}"
                    }
                }
            }
            """;
        var bookingElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = IsWeymouthTourBookingPublic(bookingElement);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsWeymouthTourBooking_ReturnsFalse_WhenItemNameMissing() {
        string jsonString = """
            {
                "availability": {
                    "item": {}
                }
            }
            """;
        var bookingElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var result = IsWeymouthTourBookingPublic(bookingElement);

        result.ShouldBeFalse();
    }

    // Helper methods to access private static methods via reflection
    private static bool HasBookingChangesPublic(Booking existingBooking, Booking updatedBooking) {
        var method = typeof(FoodTourApiController).GetMethod("HasBookingChanges",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (bool)method!.Invoke(null, [existingBooking, updatedBooking])!;
    }

    private static JsonElement? NavigateJsonPathPublic(JsonElement rootElement, string path) {
        var method = typeof(FoodTourApiController).GetMethod("NavigateJsonPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (JsonElement?)method!.Invoke(null, [rootElement, path]);
    }

    private static string ExtractStringValuePublic(JsonElement element) {
        var method = typeof(FoodTourApiController).GetMethod("ExtractStringValue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method!.Invoke(null, [element])!;
    }

    private static bool TryParseDateTimePublic(JsonElement element, out DateTime result) {
        var method = typeof(FoodTourApiController).GetMethod("TryParseDateTime",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { element, null! };
        var success = (bool)method!.Invoke(null, parameters)!;
        result = (DateTime)parameters[1];
        return success;
    }

    private static bool TryParseIntPublic(JsonElement element, out int result) {
        var method = typeof(FoodTourApiController).GetMethod("TryParseInt",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { element, 0 };
        var success = (bool)method!.Invoke(null, parameters)!;
        result = (int)parameters[1];
        return success;
    }

    private static object ValidateJsonDataPublic(JsonElement jsonData) {
        var method = typeof(FoodTourApiController).GetMethod("ValidateJsonData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return method!.Invoke(null, [jsonData])!;
    }

    private static bool IsWeymouthTourBookingPublic(JsonElement bookingElement) {
        var method = typeof(FoodTourApiController).GetMethod("IsWeymouthTourBooking",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (bool)method!.Invoke(null, [bookingElement])!;
    }
}
