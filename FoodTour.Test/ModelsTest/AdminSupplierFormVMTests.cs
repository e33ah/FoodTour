namespace FoodTour.Test.Models;

public class AdminSupplierFormVMTests {
    [Fact]
    public void Validate_ReturnsError_WhenCodeIsRequiredAndMissing() {
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = "contact@test.com",
            RequiresConfirmationCode = true,
            ConfirmationCode = string.Empty
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool result = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        result.ShouldBeFalse();
        validationResults.ShouldContain(validationResult => validationResult.MemberNames.Contains(nameof(AdminSupplierFormVM.ConfirmationCode)));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenCodeIsOptionalAndMissing() {
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = "contact@test.com",
            RequiresConfirmationCode = false,
            ConfirmationCode = string.Empty
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool result = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsError_WhenEmailNotificationsEnabledAndContactEmailMissing() {
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = string.Empty,
            EmailNotificationsEnabled = true,
            RequiresConfirmationCode = false
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool result = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        result.ShouldBeFalse();
        validationResults.ShouldContain(validationResult => validationResult.MemberNames.Contains(nameof(AdminSupplierFormVM.ContactEmail)));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenEmailNotificationsDisabledAndContactEmailMissing() {
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = string.Empty,
            EmailNotificationsEnabled = false,
            RequiresConfirmationCode = false
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool result = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        result.ShouldBeTrue();
    }
}
