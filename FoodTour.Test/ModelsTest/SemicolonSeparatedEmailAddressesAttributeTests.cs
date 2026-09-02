namespace FoodTour.Test.ModelsTest;

public class SemicolonSeparatedEmailAddressesAttributeTests {
    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidEmailAddresses() {
        var attribute = new SemicolonSeparatedEmailAddressesAttribute();
        string validEmails = "test1@example.com;test2@example.com";
        attribute.IsValid(validEmails).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForInvalidEmailAddresses() {
        var attribute = new SemicolonSeparatedEmailAddressesAttribute();
        string invalidEmails = "test1@example.com;invalid-email";
        attribute.IsValid(invalidEmails).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForEmptyString() {
        var attribute = new SemicolonSeparatedEmailAddressesAttribute();
        string emptyEmails = string.Empty;
        attribute.IsValid(emptyEmails).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForNull() {
        var attribute = new SemicolonSeparatedEmailAddressesAttribute();
        string? nullEmails = null;
        attribute.IsValid(nullEmails).ShouldBeTrue();
    }
    [Fact]
    public void IsValid_ShouldReturnFalse_ForNonStringInput() {
        var attribute = new SemicolonSeparatedEmailAddressesAttribute();
        int nonStringInput = 123;
        attribute.IsValid(nonStringInput).ShouldBeFalse();
    }

}
