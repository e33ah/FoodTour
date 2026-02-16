namespace FoodTour.Test.Models;

public class SupplierTests {
    [Theory]
    [InlineData("Test Supplier", "John Doe", "john@test.com", true)]
    [InlineData("Another Supplier", "Jane Smith", "jane@example.com", false)]
    public void Supplier_CanBeCreated_WithValidRequiredProperties(string name, string contactName, string contactEmail, bool isActive) {
        var sut = new Supplier {
            Name = name,
            ContactName = contactName,
            ContactEmail = contactEmail,
            IsActive = isActive
        };

        sut.Name.ShouldBe(name);
        sut.ContactName.ShouldBe(contactName);
        sut.ContactEmail.ShouldBe(contactEmail);
        sut.IsActive.ShouldBe(isActive);
    }

    [Theory]
    [InlineData("john@test.com", true)]
    [InlineData("jane.smith@example.co.uk", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@example.com", false)]
    [InlineData("test@", false)]
    public void Supplier_ValidatesEmailAddress_Correctly(string email, bool shouldBeValid) {
        var sut = new Supplier {
            Name = "Test Supplier",
            ContactName = "John Doe",
            ContactEmail = email,
            IsActive = true
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBe(shouldBeValid);
        if (!shouldBeValid) {
            validationResults.ShouldContain(vr => vr.MemberNames.Contains("ContactEmail"));
        }
    }

    [Fact]
    public void Supplier_RequiresName() {
        var sut = new Supplier {
            Name = null!,
            ContactName = "John Doe",
            ContactEmail = "john@test.com",
            IsActive = true
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Supplier_RequiresContactName() {
        var sut = new Supplier {
            Name = "Test Supplier",
            ContactName = null!,
            ContactEmail = "john@test.com",
            IsActive = true
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("ContactName"));
    }

    [Fact]
    public void Supplier_RequiresContactEmail() {
        var sut = new Supplier {
            Name = "Test Supplier",
            ContactName = "John Doe",
            ContactEmail = null!,
            IsActive = true
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("ContactEmail"));
    }

    [Fact]
    public void Supplier_ContactEmailProperty_HasMaxLengthAttribute() {
        var property = typeof(Supplier).GetProperty(nameof(Supplier.ContactEmail));
        var maxLengthAttribute = property!.GetCustomAttributes(typeof(MaxLengthAttribute), false)
            .Cast<MaxLengthAttribute>()
            .FirstOrDefault();

        maxLengthAttribute.ShouldNotBeNull();
        maxLengthAttribute.Length.ShouldBe(256);
    }

    [Fact]
    public void Supplier_FailsValidation_WhenNameExceedsMaxLength() {
        var sut = new Supplier {
            Name = new string('A', 201),
            ContactName = "John Doe",
            ContactEmail = "john@test.com",
            IsActive = true
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Supplier_AllowsOptionalProperties_ToBeNull() {
        var sut = new Supplier {
            Name = "Test Supplier",
            ContactName = "John Doe",
            ContactEmail = "john@test.com",
            IsActive = true,
            Address = null,
            Description = null,
            Tag1 = null,
            Tag2 = null,
            Tag3 = null,
            Tag4 = null
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBeTrue();
    }

    [Fact]
    public void Supplier_CanSetOptionalProperties() {
        var sut = new Supplier {
            Name = "Test Supplier",
            ContactName = "John Doe",
            ContactEmail = "john@test.com",
            IsActive = true,
            Address = "123 Test Street",
            Description = "A wonderful tasting experience",
            Tag1 = "Bakery",
            Tag2 = "Organic",
            Tag3 = "Local",
            Tag4 = "Artisan",
            ConfirmationCode = "venue123"
        };

        sut.Address.ShouldBe("123 Test Street");
        sut.Description.ShouldBe("A wonderful tasting experience");
        sut.Tag1.ShouldBe("Bakery");
        sut.Tag2.ShouldBe("Organic");
        sut.Tag3.ShouldBe("Local");
        sut.Tag4.ShouldBe("Artisan");
        sut.ConfirmationCode.ShouldBe("venue123");
    }
}
