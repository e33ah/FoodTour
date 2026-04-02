namespace FoodTour.Services;

public interface IBookingValidationService {
    Task<BookingValidationResult> ValidateAsync(BookingValidationRequest request);
    bool IsTourDateValid(DateTime? tourDate);
    bool IsCustomerEmailValid(string? customerEmail);
    bool IsCustomerCountValid(int? customerCount);
    bool IsCustomerNameValid(string? customerName);
}