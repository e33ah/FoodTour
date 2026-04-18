namespace FoodTour.Models;


/// <summary>
/// Validates that a value contains a semicolon-separated list of email addresses.
/// </summary>
public class SemicolonSeparatedEmailAddressesAttribute : ValidationAttribute {
    private static readonly EmailAddressAttribute emailAddressAttribute = new();

    public override bool IsValid(object? value) {
        if (value == null) {
            return true;
        }

        if (value is not string emailAddresses) {
            return false;
        }

        if (string.IsNullOrWhiteSpace(emailAddresses)) {
            return true;
        }

        string[] splitEmailAddresses = emailAddresses.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return splitEmailAddresses.Length > 0 && splitEmailAddresses.All(emailAddressAttribute.IsValid);
    }
}
