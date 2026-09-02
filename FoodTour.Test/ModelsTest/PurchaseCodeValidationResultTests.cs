namespace FoodTour.Test.Models;

public class PurchaseCodeValidationResultTests {

    [Fact]
    public void PurchaseCodeValidationResultFailure_ShouldInitializeWithDefaultValues() {
        var result = PurchaseCodeValidationResult.Failure("Invalid purchase code.");
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Invalid purchase code.");
    }
}
