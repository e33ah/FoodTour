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
            ConfirmationCode = "TOUR",
            FoodTourOfferDescription = OFFER_DESCRIPTION
        };

        sut.ApplyTo(supplier);

        supplier.FoodTourOfferDescription.ShouldBe(OFFER_DESCRIPTION);
    }
}
