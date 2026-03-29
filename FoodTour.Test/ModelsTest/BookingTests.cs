namespace FoodTour.Test.Models;

public class BookingTests {
    [Fact]
    public void Booking_InitialisesWithExpectedDefaults() {
        var sut = new Booking();

        sut.SelectedSupplierIds.ShouldNotBeNull();
        sut.SelectedSupplierIds.ShouldBeEmpty();
        sut.BookingSuppliers.ShouldNotBeNull();
        sut.BookingSuppliers.ShouldBeEmpty();
        sut.IsCancelled.ShouldBeFalse();
        sut.CustomerEmail.ShouldBe(string.Empty);
        sut.PurchaseCode.ShouldBe(string.Empty);
        sut.QrToken.ShouldNotBe(Guid.Empty);
        sut.IsConfirmed.ShouldBeFalse();
        sut.TotalGuests.ShouldBe(0);
    }

    [Fact]
    public void Booking_CreatedAt_IsSetToUtcNow()
    {
        var before = DateTime.UtcNow;

        var sut = new Booking();

        var after = DateTime.UtcNow;

        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Theory]
    [InlineData("john@test.com", true)]
    [InlineData("jane.smith@example.co.uk", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@example.com", false)]
    [InlineData("test@", false)]
    public void Booking_ValidatesEmailAddress_Correctly(string email, bool shouldBeValid) {
        var sut = new Booking {
            TourDate = DateTime.Today.AddDays(10),
            CustomerEmail = email,
            PurchaseCode = "CODE123"
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBe(shouldBeValid);
        if (!shouldBeValid) {
            validationResults.ShouldContain(vr => vr.MemberNames.Contains("CustomerEmail"));
        }
    }

    [Fact]
    public void Booking_RequiresCustomerEmail() {
        var sut = new Booking {
            TourDate = DateTime.Today.AddDays(10),
            CustomerEmail = null!,
            PurchaseCode = "CODE123"
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("CustomerEmail"));
    }

    [Fact]
    public void Booking_RequiresPurchaseCode() {
        var sut = new Booking {
            TourDate = DateTime.Today.AddDays(10),
            CustomerEmail = "a@b.com",
            PurchaseCode = null!
        };
        var validationContext = new ValidationContext(sut);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(sut, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(vr => vr.MemberNames.Contains("PurchaseCode"));
    }
}
