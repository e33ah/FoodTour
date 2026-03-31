namespace FoodTour.Controllers;

public partial class FoodTourApiController {
    private record JsonValidationResult {
        public bool IsValid { get; init; }
        public int DataLength { get; init; }
    }
}