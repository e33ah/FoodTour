namespace FoodTour.Test.Models;

public class AdminSupplierFormVMTests {
    private const string CONFIRMATION_CODE = "TOUR";

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
        validationResults.ShouldContain(validationResult => !string.IsNullOrEmpty(validationResult.ErrorMessage));
        validationResults.ShouldContain(validationResult => validationResult.ErrorMessage!.Contains("required"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenCodeIsRequiredAndWhitespace() {
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = "contact@test.com",
            RequiresConfirmationCode = true,
            ConfirmationCode = "   "
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool result = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        result.ShouldBeFalse();
        validationResults.ShouldContain(validationResult => validationResult.MemberNames.Contains(nameof(AdminSupplierFormVM.ConfirmationCode)));
        validationResults.ShouldContain(validationResult => !string.IsNullOrEmpty(validationResult.ErrorMessage));
        validationResults.ShouldContain(validationResult => validationResult.ErrorMessage!.Contains("required"));
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
        validationResults.ShouldContain(validationResult =>
            validationResult.MemberNames.Contains(nameof(AdminSupplierFormVM.ContactEmail)) &&
            validationResult.ErrorMessage == "Contact email is required when email notifications are enabled.");
    }

    [Fact]
    public void Validate_ReturnsError_WhenEmailNotificationsEnabledAndContactEmailWhitespace() {
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = "   ",
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
    public void Validate_ReturnsErrors_WhenBothRequirementsAreEnabledAndValuesAreMissing() {
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = string.Empty,
            EmailNotificationsEnabled = true,
            RequiresConfirmationCode = true,
            ConfirmationCode = string.Empty
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool result = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        result.ShouldBeFalse();
        validationResults.ShouldContain(validationResult => validationResult.MemberNames.Contains(nameof(AdminSupplierFormVM.ConfirmationCode)));
        validationResults.ShouldContain(validationResult => validationResult.MemberNames.Contains(nameof(AdminSupplierFormVM.ContactEmail)));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenBothRequirementsAreEnabledAndValuesArePresent() {
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = "contact@test.com",
            EmailNotificationsEnabled = true,
            RequiresConfirmationCode = true,
            ConfirmationCode = CONFIRMATION_CODE
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool result = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        result.ShouldBeTrue();
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

    [Fact]
    public void FromSupplier_MapsFoodTourOfferDescription() {
        const string OFFER_DESCRIPTION = "1 scone with jam and 1 pot of tea";
        var supplier = TestHelper.CreateSupplier(1, "Supplier", true, foodTourOfferDescription: OFFER_DESCRIPTION);

        var result = AdminSupplierFormVM.FromSupplier(supplier);

        result.FoodTourOfferDescription.ShouldBe(OFFER_DESCRIPTION);
    }

    [Fact]
    public void ApplyTo_MapsFoodTourOfferDescription() {
        const string OFFER_DESCRIPTION = "Small fish and chips";
        var supplier = TestHelper.CreateSupplier(1, "Supplier");
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = "contact@test.com",
            ConfirmationCode = CONFIRMATION_CODE,
            FoodTourOfferDescription = OFFER_DESCRIPTION
        };

        sut.ApplyTo(supplier);

        supplier.FoodTourOfferDescription.ShouldBe(OFFER_DESCRIPTION);
    }

    [Fact]
    public void ApplyTo_MapsWhiteSpace_ToEmptyString() {
        var supplier = TestHelper.CreateSupplier(1, "Supplier");
        var sut = new AdminSupplierFormVM {
            Name = "Supplier",
            ContactName = "Contact",
            ContactEmail = "         ",
            ConfirmationCode = "          ",
        };
        sut.ApplyTo(supplier);
        supplier.ContactEmail.ShouldBe(string.Empty);
        supplier.ConfirmationCode.ShouldBe(string.Empty);
    }

    [Fact]
    public void ApplyTo_PreservesTrimmedContactEmailAndConfirmationCode_WhenValuesAreProvided() {
        var supplier = TestHelper.CreateSupplier(1);
        var sut = new AdminSupplierFormVM {
            ContactEmail = " contact@test.com ",
            ConfirmationCode = $" {CONFIRMATION_CODE} "
        };

        sut.ApplyTo(supplier);

        supplier.ContactEmail.ShouldBe("contact@test.com");
        supplier.ConfirmationCode.ShouldBe(CONFIRMATION_CODE);
    }

    [Fact]
    public void ApplyTo_UsesEmptyStrings_WhenContactEmailAndConfirmationCodeAreNull() {
        var supplier = TestHelper.CreateSupplier(1);
        var sut = new AdminSupplierFormVM {
            ContactEmail = null!,
            ConfirmationCode = null!
        };

        sut.ApplyTo(supplier);

        supplier.ContactEmail.ShouldBe(string.Empty);
        supplier.ConfirmationCode.ShouldBe(string.Empty);
    }

    [Fact]
    public void AdminSupplierFormVM_InitializesWithDefaultValues() {
        var sut = new AdminSupplierFormVM();
        sut.Name.ShouldBe(string.Empty);
        sut.ContactName.ShouldBe(string.Empty);
        sut.ContactEmail.ShouldBe(string.Empty);
        sut.ConfirmationCode.ShouldBe(CONFIRMATION_CODE);
        sut.RequiresConfirmationCode.ShouldBeTrue();
        sut.IsActive.ShouldBeTrue();
        sut.EmailNotificationsEnabled.ShouldBeFalse();
        sut.FoodTourOfferDescription.ShouldBeNull();
    }
}
