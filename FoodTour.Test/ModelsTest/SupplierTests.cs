namespace FoodTour.Test.Models;

public class SupplierTests {
    [Theory]
    [InlineData(DayOfWeek.Monday, "Monday 9am - 5pm")]
    [InlineData(DayOfWeek.Tuesday, "Tuesday 10am - 6pm")]
    [InlineData(DayOfWeek.Wednesday, "Wednesday 11am - 7pm")]
    [InlineData(DayOfWeek.Thursday, "Thursday 12pm - 8pm")]
    [InlineData(DayOfWeek.Friday, "Friday 1pm - 9pm")]
    [InlineData(DayOfWeek.Saturday, "Saturday 2pm - 10pm")]
    [InlineData(DayOfWeek.Sunday, "Sunday 3pm - 11pm")]
    public void GetOpeningTimesForDay_ReturnsDaySpecificOpeningTimes_WhenDayHasOverride(DayOfWeek dayOfWeek, string expectedOpeningTimes) {
        var sut = TestHelper.CreateSupplier(1);
        sut.OpeningTimes = "General 9am - 5pm";
        sut.OpeningTimesMonday = "Monday 9am - 5pm";
        sut.OpeningTimesTuesday = "Tuesday 10am - 6pm";
        sut.OpeningTimesWednesday = "Wednesday 11am - 7pm";
        sut.OpeningTimesThursday = "Thursday 12pm - 8pm";
        sut.OpeningTimesFriday = "Friday 1pm - 9pm";
        sut.OpeningTimesSaturday = "Saturday 2pm - 10pm";
        sut.OpeningTimesSunday = "Sunday 3pm - 11pm";

        string? result = sut.GetOpeningTimesForDay(dayOfWeek);

        result.ShouldBe(expectedOpeningTimes);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void GetOpeningTimesForDay_ReturnsGeneralOpeningTimes_WhenDaySpecificOpeningTimesMissing(DayOfWeek dayOfWeek) {
        var sut = TestHelper.CreateSupplier(1);
        sut.OpeningTimes = "General 9am - 5pm";

        string? result = sut.GetOpeningTimesForDay(dayOfWeek);

        result.ShouldBe("General 9am - 5pm");
    }

    [Fact]
    public void GetOpeningTimesForDay_ReturnsGeneralOpeningTimes_WhenDayOfWeekIsInvalid() {
        var sut = TestHelper.CreateSupplier(1);
        sut.OpeningTimes = "General 9am - 5pm";

        string? result = sut.GetOpeningTimesForDay((DayOfWeek)(-1));

        result.ShouldBe("General 9am - 5pm");
    }

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
    [InlineData("john@test.com;jane@test.com", true)]
    [InlineData("john@test.com; jane@test.com", true)]
    [InlineData("jane.smith@example.co.uk", true)]
    [InlineData("john@test.com;invalid-email", false)]
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
    public void GetContactEmailAddresses_ReturnsTrimmedEmailAddresses_WhenMultipleAddressesConfigured() {
        var sut = TestHelper.CreateSupplier(1);
        sut.ContactEmail = "first@test.com; second@test.com ;third@test.com";

        string[] result = sut.GetContactEmailAddresses();

        result.ShouldBe(["first@test.com", "second@test.com", "third@test.com"]);
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
