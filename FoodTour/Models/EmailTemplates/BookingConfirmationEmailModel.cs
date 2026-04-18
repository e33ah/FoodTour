namespace FoodTour.Models;

public class BookingConfirmationEmailModel {
    public required string CustomerName { get; init; }
    public required string TourDate { get; init; }
    public required IReadOnlyCollection<TastingExperienceItem> TastingExperiences { get; init; }
    public required string FoodTourCodeUrl { get; init; }
    public required string UserSupportUrl { get; init; }
    public required string Subject { get; init; }
}

public class TastingExperienceItem {
    public required string Name { get; init; }
    public required string Address { get; init; }
}
